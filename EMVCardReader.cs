/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
'  
'  Module :         EMVCardReader.cs
'   
'  Description:     EMV card reading and parsing operations (separated from UI)
'  Date   :         January 2025
'==========================================================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EMVCard
{
    /// <summary>
    /// Handles EMV card reading operations separated from UI logic
    /// </summary>
    public class EMVCardReader
    {
        #region Private Fields
        private Int64 hContext;
        private Int64 hCard;
        private int retCode;
        private int Protocol;
        private bool connActive;
        private byte[] SendBuff = new byte[263];
        private byte[] RecvBuff = new byte[263];
        private int Aprotocol;
        private Int64 SendLen, RecvLen;
        private ModWinsCard64.SCARD_IO_REQUEST pioSendRequest;
        private Dictionary<string, string> labelToAID = new Dictionary<string, string>();
        #endregion

        #region Events
        public event EventHandler<string> OnMessage;
        public event EventHandler<APDUEventArgs> OnAPDUSent;
        public event EventHandler<APDUEventArgs> OnAPDUReceived;
        public event EventHandler<ErrorEventArgs> OnError;
        public event EventHandler<CardDataEventArgs> OnCardDataExtracted;
        #endregion

        #region Public Properties
        public bool IsConnected => connActive;
        public List<string> AvailableReaders { get; private set; } = new List<string>();
        public Dictionary<string, string> Applications => new Dictionary<string, string>(labelToAID);
        #endregion

        #region Initialization and Connection
        /// <summary>
        /// Initialize the smart card context and discover readers
        /// </summary>
        public bool Initialize()
        {
            try
            {
                Logger.Info("Initializing smart card context");
                ClearBuffers();
                
                int pcchReaders = 0;

                // 1. Establish Context
                retCode = ModWinsCard64.SCardEstablishContext(ModWinsCard64.SCARD_SCOPE_USER, 0, 0, ref hContext);
                if (retCode != ModWinsCard64.SCARD_S_SUCCESS)
                {
                    Logger.Error($"Failed to establish context. Return code: {retCode}");
                    OnError?.Invoke(this, new ErrorEventArgs($"Failed to establish context. Code: {retCode}"));
                    return false;
                }
                Logger.Info("Context established successfully");

                // 2. List PC/SC card readers
                retCode = ModWinsCard64.SCardListReaders(hContext, null, null, ref pcchReaders);
                if (retCode != ModWinsCard64.SCARD_S_SUCCESS)
                {
                    Logger.Error($"Failed to list readers. Return code: {retCode}");
                    OnError?.Invoke(this, new ErrorEventArgs($"Failed to list readers. Code: {retCode}"));
                    return false;
                }

                byte[] ReadersList = new byte[pcchReaders];
                retCode = ModWinsCard64.SCardListReaders(hContext, null, ReadersList, ref pcchReaders);
                if (retCode != ModWinsCard64.SCARD_S_SUCCESS)
                {
                    Logger.Error($"Failed to fill reader list. Return code: {retCode}");
                    OnError?.Invoke(this, new ErrorEventArgs($"Failed to fill reader list. Code: {retCode}"));
                    return false;
                }

                // Parse readers
                AvailableReaders.Clear();
                StringBuilder readerNameBuilder = new StringBuilder();
                int indx = 0;

                while (indx < ReadersList.Length && ReadersList[indx] != 0)
                {
                    readerNameBuilder.Clear();
                    
                    while (indx < ReadersList.Length && ReadersList[indx] != 0)
                    {
                        readerNameBuilder.Append((char)ReadersList[indx]);
                        indx++;
                    }

                    if (readerNameBuilder.Length > 0)
                    {
                        string readerName = readerNameBuilder.ToString();
                        AvailableReaders.Add(readerName);
                        Logger.Info($"Found reader: {readerName}");
                    }
                    
                    indx++;
                }

                Logger.Info($"Initialization complete. Found {AvailableReaders.Count} reader(s)");
                OnMessage?.Invoke(this, $"Found {AvailableReaders.Count} card reader(s)");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during initialization", ex);
                OnError?.Invoke(this, new ErrorEventArgs($"Initialization failed: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Connect to a specific card reader
        /// </summary>
        public bool ConnectToReader(string readerName)
        {
            try
            {
                Logger.Info($"Attempting to connect to reader: {readerName}");
                
                // Disconnect if already connected
                if (connActive)
                {
                    Logger.Info("Disconnecting from previous card");
                    ModWinsCard64.SCardDisconnect(hCard, ModWinsCard64.SCARD_UNPOWER_CARD);
                    connActive = false;
                }

                // Connect to reader
                retCode = ModWinsCard64.SCardConnect(hContext, readerName, 
                    ModWinsCard64.SCARD_SHARE_SHARED, 
                    ModWinsCard64.SCARD_PROTOCOL_T0 | ModWinsCard64.SCARD_PROTOCOL_T1, 
                    ref hCard, ref Protocol);

                if (retCode != ModWinsCard64.SCARD_S_SUCCESS)
                {
                    Logger.Error($"Failed to connect to {readerName}. Return code: {retCode}");
                    OnError?.Invoke(this, new ErrorEventArgs($"Failed to connect to reader. Code: {retCode}"));
                    return false;
                }
                
                Logger.Info($"Successfully connected to {readerName}");
                OnMessage?.Invoke(this, $"Connected to {readerName}");
                
                connActive = true;
                
                // Read and process ATR
                var atr = ReadATR();
                if (atr != null)
                {
                    OnMessage?.Invoke(this, $"ATR: {BitConverter.ToString(atr)}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during reader connection", ex);
                OnError?.Invoke(this, new ErrorEventArgs($"Connection failed: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the current card
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (connActive)
                {
                    retCode = ModWinsCard64.SCardDisconnect(hCard, ModWinsCard64.SCARD_UNPOWER_CARD);
                    Logger.Info("Disconnected from card");
                    connActive = false;
                }
                labelToAID.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during disconnect", ex);
            }
        }

        /// <summary>
        /// Release all resources and cleanup
        /// </summary>
        public void Shutdown()
        {
            try
            {
                Logger.Info("Shutting down EMV card reader");
                Disconnect();
                
                if (hContext != 0)
                {
                    ModWinsCard64.SCardReleaseContext(hContext);
                    Logger.Info("Context released");
                }
                
                AvailableReaders.Clear();
                labelToAID.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during shutdown", ex);
            }
        }
        #endregion

        #region PSE/PPSE Operations
        /// <summary>
        /// Load PSE (Payment System Environment) applications
        /// </summary>
        public bool LoadPSEApplications()
        {
            try
            {
                Logger.Info("Loading PSE (Payment System Environment)");
                labelToAID.Clear();

                if (!SelectPSEApplication())
                {
                    return false;
                }

                ReadPSERecords();
                Logger.Info($"PSE loading completed. Found {labelToAID.Count} application(s)");
                OnMessage?.Invoke(this, $"Found {labelToAID.Count} PSE applications");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during PSE loading", ex);
                OnError?.Invoke(this, new ErrorEventArgs($"PSE loading failed: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Load PPSE (Proximity Payment System Environment) applications
        /// </summary>
        public bool LoadPPSEApplications()
        {
            try
            {
                Logger.Info("Loading PPSE (Proximity Payment System Environment)");
                labelToAID.Clear();

                if (!SelectPPSEApplication())
                {
                    return false;
                }

                int appCount = ParsePPSEApplications();
                Logger.Info($"PPSE loading completed. Found {appCount} application(s)");
                OnMessage?.Invoke(this, $"Found {appCount} PPSE applications");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during PPSE loading", ex);
                OnError?.Invoke(this, new ErrorEventArgs($"PPSE loading failed: {ex.Message}"));
                return false;
            }
        }
        #endregion

        #region Card Reading Operations
        /// <summary>
        /// Read EMV application data
        /// </summary>
        public CardData ReadApplication(string applicationLabel)
        {
            try
            {
                Logger.Info($"Starting EMV application read operation for: {applicationLabel}");
                
                if (!labelToAID.ContainsKey(applicationLabel))
                {
                    Logger.Warning("No AID found for selected application");
                    OnError?.Invoke(this, new ErrorEventArgs("Please select a valid application"));
                    return null;
                }

                string aidHex = labelToAID[applicationLabel];
                Logger.Info($"Reading application with AID: {aidHex}");
                
                if (!SelectAID(aidHex))
                {
                    return null;
                }

                byte[] fciData = new byte[RecvLen];
                Array.Copy(RecvBuff, fciData, RecvLen);

                var cardData = new CardData();

                // Send GPO and automatically construct PDOL
                bool gpoSuccess = SendGPOWithAutoPDOL(fciData, RecvLen);
                if (gpoSuccess)
                {
                    Logger.Info("GPO command succeeded");
                    ParseGPOResponse(cardData);
                }
                else
                {
                    Logger.Warning("GPO command failed, trying common records");
                    OnMessage?.Invoke(this, "GPO failed, trying to read common records directly");
                    TryReadCommonRecords(cardData);
                }

                // Extract missing info from Track2 if needed
                FillMissingInfoFromTrack2(cardData);
                
                Logger.Info("EMV application read operation completed");
                OnCardDataExtracted?.Invoke(this, new CardDataEventArgs(cardData));
                
                return cardData;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during application read", ex);
                OnError?.Invoke(this, new ErrorEventArgs($"Application read failed: {ex.Message}"));
                return null;
            }
        }
        #endregion

        #region Private Helper Methods
        private void ClearBuffers()
        {
            Array.Clear(RecvBuff, 0, RecvBuff.Length);
            Array.Clear(SendBuff, 0, SendBuff.Length);
        }

        private byte[] ReadATR()
        {
            try
            {
                byte[] atr = new byte[33];
                long atrLen = atr.Length;
                int readerLen = 0;
                int state = 0;
                
                retCode = ModWinsCard64.SCardStatus(hCard, null, ref readerLen, ref state, ref Protocol, atr, ref atrLen);

                if (retCode != ModWinsCard64.SCARD_S_SUCCESS)
                {
                    Logger.Error($"Failed to retrieve ATR. Return code: {retCode}");
                    return null;
                }

                byte[] result = new byte[atrLen];
                Array.Copy(atr, result, atrLen);
                
                Logger.Info($"ATR retrieved: {BitConverter.ToString(result)}");
                
                // Determine card type
                bool isContactCard = atrLen > 0 && (atr[0] == 0x3B || atr[0] == 0x3F);
                string cardMode = isContactCard ? "contact" : "contactless";
                Logger.Info($"Card detected in {cardMode} mode");
                OnMessage?.Invoke(this, $"Card mode: {cardMode}");

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception reading ATR", ex);
                return null;
            }
        }

        private int SendAPDUandDisplay()
        {
            pioSendRequest.dwProtocol = Aprotocol;
            pioSendRequest.cbPciLength = 8;

            // Build APDU string for logging
            StringBuilder apduBuilder = new StringBuilder((int)SendLen * 3);
            for (int indx = 0; indx < SendLen; indx++)
            {
                apduBuilder.Append(' ').Append(SendBuff[indx].ToString("X2"));
            }
            
            string apduSent = apduBuilder.ToString().Trim();
            Logger.Debug($"APDU Sent: < {apduSent}");
            OnAPDUSent?.Invoke(this, new APDUEventArgs(apduSent));

            retCode = ModWinsCard64.SCardTransmit(hCard, ref pioSendRequest, SendBuff, SendLen, ref pioSendRequest, RecvBuff, ref RecvLen);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS)
            {
                Logger.Error($"APDU transmission failed. Return code: {retCode}");
                return retCode;
            }

            // Build response string for logging
            apduBuilder.Clear();
            for (int indx = 0; indx < RecvLen; indx++)
            {
                apduBuilder.Append(' ').Append(RecvBuff[indx].ToString("X2"));
            }
            
            string apduReceived = apduBuilder.ToString().Trim();
            Logger.Debug($"APDU Response: > {apduReceived}");
            OnAPDUReceived?.Invoke(this, new APDUEventArgs(apduReceived));

            return retCode;
        }

        private int TransmitWithAutoFix()
        {
            int result = SendAPDUandDisplay();

            // Case 1: SW = 6C XX, indicates Le mismatch, need to resend
            if (RecvLen == 2 && RecvBuff[0] == 0x6C)
            {
                SendBuff[SendLen - 1] = RecvBuff[1];
                RecvLen = RecvBuff[1] + 2;
                return SendAPDUandDisplay();
            }

            // Case 2: SW = 67 00, indicates missing Le, try supplementing with 0xFF and resend
            if (RecvLen == 2 && RecvBuff[0] == 0x67 && RecvBuff[1] == 0x00)
            {
                SendBuff[SendLen - 1] = 0xFF;
                RecvLen = 0xFF;
                return SendAPDUandDisplay();
            }

            // Case 3: SW = 61 XX, need GET RESPONSE
            if (RecvLen == 2 && RecvBuff[0] == 0x61)
            {
                byte le = RecvBuff[1];
                SendLen = FillBufferFromHexString($"00 C0 00 00 {le:X2}", SendBuff, 0);
                RecvLen = le + 2;
                return SendAPDUandDisplay();
            }

            return result;
        }

        private bool IsSuccessResponse()
        {
            return RecvLen >= 2 && RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00;
        }

        private bool IsRecordNotFound()
        {
            return RecvLen == 2 && RecvBuff[0] == 0x6A && RecvBuff[1] == 0x83;
        }

        private int FillBufferFromHexString(string hexString, byte[] buffer, int startIndex)
        {
            if (string.IsNullOrWhiteSpace(hexString))
                throw new ArgumentException("Input string cannot be empty", nameof(hexString));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (startIndex < 0 || startIndex >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Start position exceeds buffer range");

            string[] hexValues = hexString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexValues.Length;

            if (startIndex + byteCount > buffer.Length)
                throw new ArgumentException("Buffer not large enough, cannot accommodate all data");

            for (int i = 0; i < byteCount; i++)
            {
                if (!byte.TryParse(hexValues[i], System.Globalization.NumberStyles.HexNumber, null, out byte result))
                    throw new FormatException($"Unable to parse '{hexValues[i]}' as hexadecimal byte");
                buffer[startIndex + i] = result;
            }

            return byteCount;
        }

        private bool SelectAID(string aidHex)
        {
            string[] aidBytes = aidHex.Split(' ');
            string selectAID = $"00 A4 04 00 {aidBytes.Length:X2} {aidHex}";
            SendLen = FillBufferFromHexString(selectAID, SendBuff, 0);
            RecvLen = 0xFF;

            int result = TransmitWithAutoFix();
            if (result != 0 || !IsSuccessResponse())
            {
                Logger.Error($"Failed to select AID: {aidHex}");
                OnError?.Invoke(this, new ErrorEventArgs("Failed to select AID"));
                return false;
            }
            
            Logger.Info("AID selected successfully");
            return true;
        }

        private bool SelectPSEApplication()
        {
            string selectPSE = "00 A4 04 00 0E 31 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix();
            
            if (result != 0)
            {
                Logger.Error("Failed to select PSE application");
                OnError?.Invoke(this, new ErrorEventArgs("Failed to select PSE application"));
                return false;
            }
            
            Logger.Info("PSE application selected successfully");
            return true;
        }

        private bool SelectPPSEApplication()
        {
            string selectPPSE = "00 A4 04 00 0E 32 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix();
            
            if (result != 0 || !IsSuccessResponse())
            {
                Logger.Error("Failed to select PPSE application");
                OnError?.Invoke(this, new ErrorEventArgs("Failed to select PPSE application"));
                return false;
            }
            
            Logger.Info("PPSE application selected successfully");
            return true;
        }

        private void ReadPSERecords()
        {
            for (int record = 1; ; record++)
            {
                string readSFI = $"00 B2 {record:X2} 0C 00";
                SendLen = FillBufferFromHexString(readSFI, SendBuff, 0);
                RecvLen = 0xFF;

                int result = TransmitWithAutoFix();

                if (IsRecordNotFound())
                {
                    Logger.Info($"PSE Record {record} does not exist, finished reading AIDs");
                    break;
                }

                if (result != 0 || !IsSuccessResponse())
                {
                    Logger.Warning($"Failed to read PSE Record {record}");
                    break;
                }

                Logger.Info($"Parsing AID information from Record {record}");
                ParseSFIRecord(RecvBuff, RecvLen - 2);
            }
        }

        private int ParsePPSEApplications()
        {
            int index = 0;
            int appCount = 0;
            
            while (index < RecvLen - 2)
            {
                if (RecvBuff[index] != 0x61)
                {
                    index++;
                    continue;
                }

                int len = RecvBuff[index + 1];
                int start = index + 2;
                int end = start + len;
                
                if (end > RecvLen - 2)
                {
                    break;
                }

                var (aid, label) = ParseApplicationTemplate(start, end);

                if (!string.IsNullOrEmpty(aid))
                {
                    if (string.IsNullOrEmpty(label))
                    {
                        label = "App_" + aid.Substring(Math.Max(0, aid.Length - 4));
                    }
                    
                    string itemName = $"{appCount + 1}. {label}";
                    if (!labelToAID.ContainsKey(itemName))
                    {
                        labelToAID[itemName] = aid;
                        Logger.Info($"Found application: {label} with AID: {aid}");
                        appCount++;
                    }
                }

                index = end;
            }
            
            return appCount;
        }

        // Additional helper methods for parsing would go here...
        // (I'll include the key ones but truncate for space)

        private (string aid, string label) ParseApplicationTemplate(int start, int end)
        {
            string currentAID = "";
            string label = "";
            int subIndex = start;

            while (subIndex < end)
            {
                byte tag = RecvBuff[subIndex++];
                if (subIndex >= end) break;
                
                int tagLen = RecvBuff[subIndex++];
                if (subIndex + tagLen > end) break;
                
                byte[] value = new byte[tagLen];
                Array.Copy(RecvBuff, subIndex, value, 0, tagLen);
                subIndex += tagLen;

                if (tag == 0x4F)
                {
                    currentAID = string.Join(" ", value.Select(b => b.ToString("X2")));
                    OnMessage?.Invoke(this, $"AID: {currentAID}");
                }
                else if (tag == 0x50)
                {
                    label = Encoding.ASCII.GetString(value).Trim();
                    OnMessage?.Invoke(this, $"Application Label: {label}");
                }
            }

            return (currentAID, label);
        }

        private void ParseSFIRecord(byte[] buffer, long length)
        {
            string currentAID = "";
            int index = 0;
            
            while (index < length)
            {
                byte tag = buffer[index++];
                byte? tag2 = null;

                if ((tag & 0x1F) == 0x1F)
                {
                    tag2 = buffer[index++];
                }

                int tagValue = tag2.HasValue ? (tag << 8 | tag2.Value) : tag;

                int len = buffer[index++];
                if (len > 0x80)
                {
                    int lenLen = len & 0x7F;
                    len = 0;
                    for (int i = 0; i < lenLen; i++)
                        len = (len << 8) + buffer[index++];
                }

                byte[] value = new byte[len];
                Array.Copy(buffer, index, value, 0, len);
                index += len;

                switch (tagValue)
                {
                    case 0x4F: // AID
                        currentAID = string.Join(" ", value.Select(b => b.ToString("X2")));
                        OnMessage?.Invoke(this, $"AID: {currentAID}");
                        break;
                    case 0x50: // Application Label
                        string label = Encoding.ASCII.GetString(value);
                        OnMessage?.Invoke(this, $"Application Label: {label}");
                        if (!string.IsNullOrEmpty(currentAID))
                        {
                            labelToAID[label] = currentAID;
                        }
                        break;
                    case 0x9F12: // Preferred Name
                        OnMessage?.Invoke(this, $"Preferred Name: {Encoding.ASCII.GetString(value)}");
                        break;
                    case 0x87: // Priority
                        OnMessage?.Invoke(this, $"Application Priority: {value[0]}");
                        break;
                    case 0x61: // Application Template, recursively parse
                    case 0x70: // FCI template
                        ParseSFIRecord(value, len);
                        break;
                }
            }
        }

        // Additional methods like SendGPOWithAutoPDOL, ParseGPOResponse, etc.

        private bool SendGPOWithAutoPDOL(byte[] fciBuffer, long fciLen)
        {
            int index = 0;
            while (index < fciLen - 2)
            {
                if (fciBuffer[index] == 0x9F && fciBuffer[index + 1] == 0x38)
                {
                    index += 2;
                    int len = fciBuffer[index++];
                    byte[] pdolRaw = new byte[len];
                    Array.Copy(fciBuffer, index, pdolRaw, 0, len);

                    int pdolIndex = 0;
                    List<byte> pdolData = new List<byte>();
                    while (pdolIndex < pdolRaw.Length)
                    {
                        int tag = pdolRaw[pdolIndex++];
                        if ((tag & 0x1F) == 0x1F)
                        {
                            tag = (tag << 8) | pdolRaw[pdolIndex++];
                        }

                        int tagLen = pdolRaw[pdolIndex++];

                        // Fill various PDOL data
                        switch (tag)
                        {
                            case 0x9F66: // TTQ
                                pdolData.AddRange(new byte[] { 0x37, 0x00, 0x00, 0x00 });
                                break;
                            case 0x9F02: // Amount Authorized
                                pdolData.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 });
                                break;
                            case 0x9F03: // Amount Other (Cashback)
                                pdolData.AddRange(new byte[tagLen]);
                                break;
                            case 0x9F1A: // Terminal Country Code (China: 0156)
                            case 0x5F2A: // Transaction Currency Code (RMB: 0156)
                                pdolData.AddRange(new byte[] { 0x01, 0x56 });
                                break;
                            case 0x9A: // Transaction Date (YYMMDD)
                                var date = DateTime.Now;
                                pdolData.AddRange(new byte[] {
                                    (byte)(date.Year % 100),
                                    (byte)(date.Month),
                                    (byte)(date.Day)
                                });
                                break;
                            case 0x9C: // Transaction Type (default: Purchase)
                                pdolData.Add(0x00);
                                break;
                            case 0x9F37: // Unpredictable Number
                                var rnd = new Random();
                                for (int i = 0; i < tagLen; i++)
                                {
                                    pdolData.Add((byte)rnd.Next(0, 256));
                                }
                                break;
                            default:
                                pdolData.AddRange(new byte[tagLen]); // Fill with 0
                                break;
                        }
                    }

                    int pdolDataLen = pdolData.Count;
                    List<byte> gpo = new List<byte> {
                        0x80, 0xA8, 0x00, 0x00,
                        (byte)(pdolDataLen + 2), 0x83, (byte)pdolDataLen
                    };
                    gpo.AddRange(pdolData);
                    gpo.Add(0x00); // Le

                    for (int i = 0; i < gpo.Count; i++)
                        SendBuff[i] = gpo[i];
                    SendLen = gpo.Count;
                    RecvLen = 0xFF;
                    int result = TransmitWithAutoFix();
                    if (result != 0)
                        return false;

                    if (RecvBuff[0] == 0x80 || RecvBuff[0] == 0x77)
                    {
                        OnMessage?.Invoke(this, "GPO returned successfully (with PDOL)");
                        return true;
                    }
                    else
                    {
                        OnMessage?.Invoke(this, "GPO return format abnormal");
                        return false;
                    }
                }
                else
                {
                    index++;
                }
            }

            // === No PDOL, send simplified GPO ===
            string gpoEmpty = "80 A8 00 00 02 83 00 00";
            SendLen = FillBufferFromHexString(gpoEmpty, SendBuff, 0);
            RecvLen = 0xFF;
            int res = TransmitWithAutoFix();
            if (res != 0)
                return false;

            if (RecvBuff[0] == 0x80 || RecvBuff[0] == 0x77)
            {
                OnMessage?.Invoke(this, "GPO returned successfully (simplified mode)");
                return true;
            }
            else
            {
                OnMessage?.Invoke(this, "Simplified GPO return format abnormal");
                return false;
            }
        }

        private void ParseGPOResponse(CardData cardData)
        {
            // Parse GPO response directly for Track2, PAN and other TLV fields
            ParseTLV(RecvBuff, 0, (int)RecvLen - 2, cardData, 0, true);

            // Parse AFL and read records
            var aflList = ParseAFL(RecvBuff, RecvLen);
            if (aflList.Count > 0)
            {
                Logger.Info($"Found {aflList.Count} AFL entries");
                ReadAFLRecords(aflList, cardData);
            }
            else
            {
                OnMessage?.Invoke(this, "Unable to parse AFL, trying to read SFI 1 Record 1");
                Logger.Warning("No AFL found, trying common records");
                TryReadCommonRecords(cardData);
            }
        }

        private void TryReadCommonRecords(CardData cardData)
        {
            // Common SFI and record combinations
            var commonRecords = new[] {
                (sfi: 1, rec: 1),
                (sfi: 2, rec: 1),
                (sfi: 3, rec: 1),
                (sfi: 4, rec: 1),
                (sfi: 1, rec: 2),
                (sfi: 2, rec: 2)
            };

            foreach (var (sfi, rec) in commonRecords)
            {
                if (TryReadRecord(sfi, rec))
                {
                    OnMessage?.Invoke(this, $"Successfully read SFI {sfi} Record {rec}");
                    ParseRecordContent(RecvBuff, RecvLen - 2, cardData);
                }
            }
        }

        private bool TryReadRecord(int sfi, int rec)
        {
            string cmd = $"00 B2 {rec:X2} {((sfi << 3) | 4):X2} 00";
            SendLen = FillBufferFromHexString(cmd, SendBuff, 0);
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix();

            if (result != 0 || !IsSuccessResponse())
            {
                Logger.Warning($"Failed to read SFI {sfi} Record {rec}");
                return false;
            }

            Logger.Info($"Successfully read SFI {sfi} Record {rec}");
            return true;
        }

        private void ReadAFLRecords(List<(int sfi, int startRecord, int endRecord)> aflList, CardData cardData)
        {
            foreach (var (sfi, start, end) in aflList)
            {
                for (int rec = start; rec <= end; rec++)
                {
                    if (TryReadRecord(sfi, rec))
                    {
                        ParseRecordContent(RecvBuff, RecvLen - 2, cardData);
                    }
                }
            }
        }

        private List<(int sfi, int startRecord, int endRecord)> ParseAFL(byte[] buffer, long length)
        {
            var aflList = new List<(int, int, int)>();

            if (buffer[0] == 0x77)  // GPO return template 77
            {
                int i = 0;
                while (i < length - 2)
                {
                    if (buffer[i] == 0x94)
                    {
                        int len = buffer[i + 1];
                        int pos = i + 2;
                        while (pos + 3 < i + 2 + len)
                        {
                            int sfi = buffer[pos] >> 3;
                            int start = buffer[pos + 1];
                            int end = buffer[pos + 2];
                            aflList.Add((sfi, start, end));
                            pos += 4;
                        }
                        break;
                    }
                    i++;
                }
            }
            else if (buffer[0] == 0x80)  // GPO return template 80 (Visa)
            {
                int totalLen = buffer[1];
                if (totalLen + 2 > buffer.Length)
                    return aflList;

                int pos = 2;
                pos += 2; // Skip AIP (2 bytes)

                while (pos + 3 < 2 + totalLen)
                {
                    int sfi = buffer[pos] >> 3;
                    int start = buffer[pos + 1];
                    int end = buffer[pos + 2];
                    if (sfi >= 1 && sfi <= 31 && start >= 1 && end >= start)
                    {
                        aflList.Add((sfi, start, end));
                    }
                    pos += 4;
                }
            }

            return aflList;
        }

        private void ParseRecordContent(byte[] buffer, long len, CardData cardData)
        {
            // Check if template format (starts with 70)
            if (buffer[0] == 0x70)
            {
                int templateLen = buffer[1];
                int startPos = 2;

                // Handle long format length
                if (buffer[1] > 0x80)
                {
                    int lenBytes = buffer[1] & 0x7F;
                    templateLen = 0;
                    for (int i = 0; i < lenBytes; i++)
                    {
                        templateLen = (templateLen << 8) | buffer[2 + i];
                    }
                    startPos = 2 + lenBytes;
                }

                // Parse record file with high priority (1)
                ParseTLV(buffer, 0, (int)len, cardData, 1, true);
            }
            else
            {
                // Directly parse TLV data with high priority
                ParseTLV(buffer, 0, (int)len, cardData, 1, true);
            }
        }

        private string ParseTLV(byte[] buffer, int startIndex, int endIndex, CardData cardData, int priority = 0, bool storeTrack2 = true)
        {
            string track2Data = null;
            int index = startIndex;

            while (index < endIndex)
            {
                if (index >= buffer.Length)
                    break;

                // Parse Tag
                byte tag = buffer[index++];
                byte? tag2 = null;

                if ((tag & 0x1F) == 0x1F)
                {
                    if (index >= buffer.Length)
                        break;
                    tag2 = buffer[index++];
                }

                int tagValue = tag2.HasValue ? (tag << 8 | tag2.Value) : tag;

                // Parse Length
                if (index >= buffer.Length)
                    break;

                int len = buffer[index++];
                if (len >= 0x80)
                {
                    int lenLen = (len & 0x7F);

                    if (lenLen <= 0 || lenLen > 3 || index + lenLen > buffer.Length)
                    {
                        OnMessage?.Invoke(this, $"TLV length field abnormal: lenLen={lenLen}, index={index}");
                        break;
                    }

                    len = 0;
                    for (int i = 0; i < lenLen; i++)
                    {
                        len = (len << 8) + buffer[index++];
                    }
                }

                // Safety check
                if (len < 0 || len > 4096 || index + len > buffer.Length)
                {
                    OnMessage?.Invoke(this, $"TLV length illegal: len={len}, index={index}");
                    break;
                }

                // Extract Value
                byte[] value = new byte[len];
                Array.Copy(buffer, index, value, 0, len);
                index += len;

                // Process data based on Tag
                switch (tagValue)
                {
                    case 0x5A: // PAN (Card Number)
                        if (priority > 0 || string.IsNullOrEmpty(cardData.PAN))
                        {
                            string pan = BitConverter.ToString(value).Replace("-", "");
                            pan = pan.TrimEnd('F');
                            cardData.PAN = pan;
                            OnMessage?.Invoke(this, $"Card Number (PAN): {pan}");
                        }
                        break;

                    case 0x5F24: // Expiry Date
                        if (priority > 0 || string.IsNullOrEmpty(cardData.ExpiryDate))
                        {
                            string rawDate = BitConverter.ToString(value).Replace("-", "");
                            string expiry = "";

                            if (rawDate.Length >= 6)
                            {
                                expiry = $"20{rawDate.Substring(0, 2)}-{rawDate.Substring(2, 2)}-{rawDate.Substring(4, 2)}";
                            }
                            else if (rawDate.Length >= 4)
                            {
                                expiry = $"20{rawDate.Substring(0, 2)}-{rawDate.Substring(2, 2)}";
                            }

                            if (!string.IsNullOrEmpty(expiry))
                            {
                                cardData.ExpiryDate = expiry;
                                OnMessage?.Invoke(this, $"Expiry Date: {expiry}");
                            }
                        }
                        break;

                    case 0x5F20: // Cardholder Name
                        if (priority > 0 || string.IsNullOrEmpty(cardData.CardholderName))
                        {
                            string name = Encoding.ASCII.GetString(value).Trim();
                            cardData.CardholderName = name;
                            OnMessage?.Invoke(this, $"Cardholder Name: {name}");
                        }
                        break;

                    case 0x57: // Track2 Data
                        string track2 = BitConverter.ToString(value).Replace("-", "");
                        if (storeTrack2)
                        {
                            cardData.Track2Data = track2;
                            track2Data = track2;
                            OnMessage?.Invoke(this, $"Track2 Data: {track2}");
                        }
                        break;

                    case 0x9F6B: // Track2 Equivalent Data
                        if (string.IsNullOrEmpty(cardData.Track2Data))
                        {
                            string track2Equiv = BitConverter.ToString(value).Replace("-", "");
                            if (storeTrack2)
                            {
                                cardData.Track2Data = track2Equiv;
                                track2Data = track2Equiv;
                                OnMessage?.Invoke(this, $"Track2 Equivalent Data: {track2Equiv}");
                            }
                        }
                        break;

                    case 0x70: // Record Template
                    case 0x77: // Response Message Template Format 2
                        ParseTLV(value, 0, value.Length, cardData, priority, storeTrack2);
                        break;

                    case 0x80: // Response Message Template Format 1
                        if (len > 2) // Ensure sufficient data
                        {
                            // Skip AIP (2 bytes)
                            ParseTLV(value, 2, value.Length, cardData, priority, storeTrack2);
                        }
                        break;
                }
            }

            return track2Data;
        }

        private void FillMissingInfoFromTrack2(CardData cardData)
        {
            // Only extract from Track2 when card number or expiry date is empty
            if (string.IsNullOrEmpty(cardData.Track2Data))
            {
                return;
            }

            string track2 = cardData.Track2Data;
            bool cardNumEmpty = string.IsNullOrEmpty(cardData.PAN);
            bool expiryEmpty = string.IsNullOrEmpty(cardData.ExpiryDate);

            // If both fields are filled, no need to continue
            if (!cardNumEmpty && !expiryEmpty)
            {
                return;
            }

            // Look for separator "D"
            int dIndex = track2.IndexOf('D');

            if (dIndex > 0 && track2.Length >= dIndex + 5)
            {
                ExtractFromTrack2WithSeparator(track2, dIndex, cardNumEmpty, expiryEmpty, "D", cardData);
            }
            else
            {
                // Handle case without clear separator
                dIndex = track2.IndexOf('=');
                if (dIndex > 0 && track2.Length >= dIndex + 5)
                {
                    ExtractFromTrack2WithSeparator(track2, dIndex, cardNumEmpty, expiryEmpty, "=", cardData);
                }
                else
                {
                    // Try parsing using fixed positions
                    TryExtractFromFixedFormat(track2, cardNumEmpty, expiryEmpty, cardData);
                }
            }

            // If still unable to parse, log warning
            if (string.IsNullOrEmpty(cardData.PAN))
            {
                OnMessage?.Invoke(this, $"Warning: Unable to parse card number from Track2 data: {track2}");
            }
        }

        private void ExtractFromTrack2WithSeparator(string track2, int separatorIndex, bool extractCard, bool extractExpiry, string separator, CardData cardData)
        {
            if (extractCard)
            {
                string pan = track2.Substring(0, separatorIndex).TrimEnd('F');
                cardData.PAN = pan;
                OnMessage?.Invoke(this, $"Extracted card number from Track2{(separator == "D" ? "" : $" ({separator} separator)")}: {pan}");
            }

            if (extractExpiry && track2.Length >= separatorIndex + 5)
            {
                string expiryYYMM = track2.Substring(separatorIndex + 1, 4);
                if (Regex.IsMatch(expiryYYMM, @"^\d{4}$"))
                {
                    string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                    cardData.ExpiryDate = expiry;
                    OnMessage?.Invoke(this, $"Extracted expiry date from Track2{(separator == "D" ? "" : $" ({separator} separator)")}: {expiry}");
                }
            }
        }

        private void TryExtractFromFixedFormat(string track2, bool extractCard, bool extractExpiry, CardData cardData)
        {
            // Try extracting PAN (assume PAN length is 16-19 digits)
            if (extractCard)
            {
                for (int panLength = 16; panLength <= 19; panLength++)
                {
                    if (track2.Length >= panLength)
                    {
                        string possiblePan = track2.Substring(0, panLength);
                        if (Regex.IsMatch(possiblePan, @"^\d+$"))
                        {
                            cardData.PAN = possiblePan;
                            OnMessage?.Invoke(this, $"Extracted card number from Track2 (fixed format): {possiblePan}");

                            // Try extracting expiry date
                            if (extractExpiry && track2.Length >= panLength + 4)
                            {
                                string expiryYYMM = track2.Substring(panLength, 4);
                                if (Regex.IsMatch(expiryYYMM, @"^\d{4}$"))
                                {
                                    string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                                    cardData.ExpiryDate = expiry;
                                    OnMessage?.Invoke(this, $"Extracted expiry date from Track2 (fixed format): {expiry}");
                                }
                            }
                            return;
                        }
                    }
                }

                // Try special formats (UnionPay)
                TryExtractUnionPayFormat(track2, extractExpiry, cardData);
            }
        }

        private void TryExtractUnionPayFormat(string track2, bool extractExpiry, CardData cardData)
        {
            if (track2.Length < 30)
            {
                return;
            }

            string possiblePan = track2.Substring(0, 16);
            if (possiblePan.StartsWith("62") || possiblePan.StartsWith("60"))
            {
                cardData.PAN = possiblePan;
                OnMessage?.Invoke(this, $"Extracted card number from Track2 (UnionPay special format): {possiblePan}");

                if (extractExpiry && track2.Length >= 20)
                {
                    for (int i = 16; i <= 20 && i + 4 <= track2.Length; i++)
                    {
                        string possibleExpiry = track2.Substring(i, 4);
                        if (Regex.IsMatch(possibleExpiry, @"^[\dD=]{4}$"))
                        {
                            string expiryYYMM = possibleExpiry.Replace("D", "").Replace("=", "");
                            if (expiryYYMM.Length == 4 && Regex.IsMatch(expiryYYMM, @"^\d{4}$"))
                            {
                                string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                                cardData.ExpiryDate = expiry;
                                OnMessage?.Invoke(this, $"Extracted expiry date from Track2 (UnionPay special format): {expiry}");
                                break;
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }

    #region Event Args Classes
    public class APDUEventArgs : EventArgs
    {
        public string APDU { get; }
        public APDUEventArgs(string apdu) => APDU = apdu;
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Error { get; }
        public ErrorEventArgs(string error) => Error = error;
    }

    public class CardDataEventArgs : EventArgs
    {
        public CardData Data { get; }
        public CardDataEventArgs(CardData data) => Data = data;
    }
    #endregion

    #region Data Classes
    /// <summary>
    /// Represents extracted EMV card data
    /// </summary>
    public class CardData
    {
        public string PAN { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string CardholderName { get; set; } = "";
        public string Track2Data { get; set; } = "";
        public string AID { get; set; } = "";
        public string ApplicationLabel { get; set; } = "";
        
        public bool IsEmpty => string.IsNullOrEmpty(PAN) && 
                              string.IsNullOrEmpty(ExpiryDate) && 
                              string.IsNullOrEmpty(CardholderName) && 
                              string.IsNullOrEmpty(Track2Data);
    }
    #endregion
}