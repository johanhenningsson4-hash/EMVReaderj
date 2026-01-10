/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
'  
'  Author :         Eternal TUTU
'
'  Module :         EMVReader.cs
'   
'  Date   :         June 23, 2008
'==========================================================================================*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EMVCard
{
    public partial class MainEMVReaderBin : Form
    {
        public Int64 hContext, hCard;
        public int retCode, Protocol;
        public bool connActive, validATS;
        public bool autoDet;
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public int reqType, Aprotocol, dwProtocol, cbPciLength;
        public Int64 SendLen, RecvLen, nBytesRet;
        public ModWinsCard64.SCARD_IO_REQUEST pioSendRequest;
        private Dictionary<string, string> labelToAID = new Dictionary<string, string>();


        public MainEMVReaderBin() {
            InitializeComponent();
            Logger.Initialize();
            Logger.Info("EMV Reader application started");
        }

        private void ClearBuffers() {
            Array.Clear(RecvBuff, 0, RecvBuff.Length);
            Array.Clear(SendBuff, 0, SendBuff.Length);
        }

        private void displayOut(int errType, int retVal, string PrintText) {
            switch (errType) {
                case 0:
                    Logger.Info(PrintText);
                    break;
                case 1:
                    PrintText = ModWinsCard64.GetScardErrMsg(retVal);
                    Logger.Error($"Error Code {retVal}: {PrintText}");
                    break;
                case 2:
                    PrintText = "<" + PrintText;
                    Logger.Debug($"APDU Sent: {PrintText}");
                    break;
                case 3:
                    PrintText = "> " + PrintText;
                    Logger.Debug($"APDU Response: {PrintText}");
                    break;
            }

            richTextBoxLogs.Select(richTextBoxLogs.Text.Length, 0);
            richTextBoxLogs.SelectedText = PrintText + "\r\n";
            richTextBoxLogs.ScrollToCaret();
        }

        private void EnableButtons() {
            bInit.Enabled = false;
            bConnect.Enabled = true;
            bReset.Enabled = true;
            bClear.Enabled = true;
        }

        private void bInit_Click(object sender, EventArgs e) {
            Logger.Info("Initializing smart card context");
            int pcchReaders = 0;

            // 1. Establish Context
            retCode = ModWinsCard64.SCardEstablishContext(ModWinsCard64.SCARD_SCOPE_USER, 0, 0, ref hContext);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                Logger.Error($"Failed to establish context. Return code: {retCode}");
                displayOut(1, retCode, "");
                return;
            }
            Logger.Info("Context established successfully");

            // 2. List PC/SC card readers installed in the system
            retCode = ModWinsCard64.SCardListReaders(this.hContext, null, null, ref pcchReaders);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                Logger.Error($"Failed to list readers. Return code: {retCode}");
                displayOut(1, retCode, "");
                return;
            }

            EnableButtons();

            byte[] ReadersList = new byte[pcchReaders];

            // Fill reader list
            retCode = ModWinsCard64.SCardListReaders(this.hContext, null, ReadersList, ref pcchReaders);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                Logger.Error($"Failed to fill reader list. Return code: {retCode}");
                displayOut(1, retCode, "");
                return;
            }

            // Convert reader buffer to string
            StringBuilder readerNameBuilder = new StringBuilder();
            int indx = 0;

            while (indx < ReadersList.Length && ReadersList[indx] != 0) {
                readerNameBuilder.Clear();
                
                while (indx < ReadersList.Length && ReadersList[indx] != 0) {
                    readerNameBuilder.Append((char)ReadersList[indx]);
                    indx++;
                }

                if (readerNameBuilder.Length > 0) {
                    string readerName = readerNameBuilder.ToString();
                    cbReader.Items.Add(readerName);
                    Logger.Info($"Found reader: {readerName}");
                }
                
                indx++;
            }

            if (cbReader.Items.Count > 0)
                cbReader.SelectedIndex = 0;
            
            Logger.Info($"Initialization complete. Found {cbReader.Items.Count} reader(s)");
        }

        private int SendAPDUandDisplay() {
            pioSendRequest.dwProtocol = Aprotocol;
            pioSendRequest.cbPciLength = 8;

            // Display Apdu In
            StringBuilder apduBuilder = new StringBuilder((int)SendLen * 3);
            for (int indx = 0; indx < SendLen; indx++) {
                apduBuilder.Append(' ').Append(SendBuff[indx].ToString("X2"));
            }
            displayOut(2, 0, apduBuilder.ToString());

            retCode = ModWinsCard64.SCardTransmit(hCard, ref pioSendRequest, SendBuff, SendLen, ref pioSendRequest, RecvBuff, ref RecvLen);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                displayOut(1, retCode, "");
                return retCode;
            }

            // Display Apdu Response
            apduBuilder.Clear();
            for (int indx = 0; indx < RecvLen; indx++) {
                apduBuilder.Append(' ').Append(RecvBuff[indx].ToString("X2"));
            }
            displayOut(3, 0, apduBuilder.ToString().Trim());

            return retCode;
        }

        private void bReadApp_Click(object sender, EventArgs e) {
            Logger.Info("Starting EMV application read operation");
            ClearCardTextFields();

            string selectedLabel = cbPSE.Text.Trim();
            if (!labelToAID.ContainsKey(selectedLabel)) {
                displayOut(0, 0, "Please select an application AID");
                Logger.Warning("No AID selected for read operation");
                return;
            }

            string aidHex = labelToAID[selectedLabel];
            Logger.Info($"Reading application with AID: {aidHex}");
            
            if (!SelectAID(aidHex)) {
                return;
            }

            byte[] fciData = new byte[RecvLen];
            Array.Copy(RecvBuff, fciData, RecvLen);

            // Send GPO and automatically construct PDOL
            bool gpoSuccess = SendGPOWithAutoPDOL(fciData, RecvLen);
            if (!gpoSuccess) {
                displayOut(0, 0, "Failed to send GPO");
                Logger.Warning("GPO command failed");
            }
            else {
                Logger.Info("GPO command succeeded");
            }

            // Parse data based on GPO success
            if (gpoSuccess) {
                ParseGPOResponse();
            }
            else {
                displayOut(0, 0, "Due to GPO failure, trying to read common records directly");
                Logger.Warning("Attempting to read common records after GPO failure");
                TryReadCommonRecords();
            }

            // Finally, supplement information from Track2
            FillMissingInfoFromTrack2();
            Logger.Info("EMV application read operation completed");
        }

        private void ClearCardTextFields() {
            textCardNum.Text = "";
            textEXP.Text = "";
            textHolder.Text = "";
            textTrack.Text = "";
        }

        private bool SelectAID(string aidHex) {
            string[] aidBytes = aidHex.Split(' ');
            string selectAID = $"00 A4 04 00 {aidBytes.Length:X2} {aidHex}";
            SendLen = FillBufferFromHexString(selectAID, SendBuff, 0);
            RecvLen = 0xFF;

            int result = TransmitWithAutoFix();
            if (result != 0 || !IsSuccessResponse()) {
                displayOut(0, 0, "Failed to select AID");
                Logger.Error($"Failed to select AID: {aidHex}");
                return false;
            }
            
            Logger.Info("AID selected successfully");
            return true;
        }

        private bool IsSuccessResponse() {
            return RecvLen >= 2 && RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00;
        }

        private void ParseGPOResponse() {
            // First try to parse GPO response directly for Track2, PAN and other TLV fields
            ParseTLV(RecvBuff, 0, (int)RecvLen - 2, 0, true);

            // Then parse AFL and read records
            var aflList = ParseAFL(RecvBuff, RecvLen);
            if (aflList.Count > 0) {
                Logger.Info($"Found {aflList.Count} AFL entries");
                ReadAFLRecords(aflList);
            }
            else {
                displayOut(0, 0, "Unable to parse AFL, trying to read SFI 1 Record 1");
                Logger.Warning("No AFL found, trying common records");
                TryReadCommonRecords();
            }
        }

        private void ReadAFLRecords(List<(int sfi, int startRecord, int endRecord)> aflList) {
            foreach (var (sfi, start, end) in aflList) {
                for (int rec = start; rec <= end; rec++) {
                    if (TryReadRecord(sfi, rec)) {
                        ParseRecordContent(RecvBuff, RecvLen - 2);
                    }
                }
            }
        }

        private bool TryReadRecord(int sfi, int rec) {
            string cmd = $"00 B2 {rec:X2} {((sfi << 3) | 4):X2} 00";
            SendLen = FillBufferFromHexString(cmd, SendBuff, 0);
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix();
            
            if (result != 0 || !IsSuccessResponse()) {
                displayOut(0, 0, $"SFI {sfi} Record {rec} did not return 90 00, skipping parse");
                Logger.Warning($"Failed to read SFI {sfi} Record {rec}");
                return false;
            }
            
            Logger.Info($"Successfully read SFI {sfi} Record {rec}");
            return true;
        }

        // New method: Try reading common SFI and records
        private void TryReadCommonRecords() {
            // Common SFI and record combinations
            var commonRecords = new[] {
                (sfi: 1, rec: 1),
                (sfi: 2, rec: 1),
                (sfi: 3, rec: 1),
                (sfi: 4, rec: 1),
                (sfi: 1, rec: 2),
                (sfi: 2, rec: 2)
            };

            foreach (var (sfi, rec) in commonRecords) {
                if (TryReadRecord(sfi, rec)) {
                    displayOut(0, 0, $"Successfully read SFI {sfi} Record {rec}");
                    ParseRecordContent(RecvBuff, RecvLen - 2);
                }
            }
        }


        private void FillMissingInfoFromTrack2() {
            // Only extract from Track2 when card number or expiry date is empty
            if (string.IsNullOrEmpty(textTrack.Text)) {
                return;
            }

            string track2 = textTrack.Text;
            bool cardNumEmpty = string.IsNullOrEmpty(textCardNum.Text);
            bool expiryEmpty = string.IsNullOrEmpty(textEXP.Text);

            // If both fields are filled, no need to continue
            if (!cardNumEmpty && !expiryEmpty) {
                return;
            }

            // Look for separator "D"
            int dIndex = track2.IndexOf('D');

            if (dIndex > 0 && track2.Length >= dIndex + 5) {
                ExtractFromTrack2WithSeparator(track2, dIndex, cardNumEmpty, expiryEmpty, "D");
            }
            else {
                // Handle case without clear separator
                dIndex = track2.IndexOf('=');
                if (dIndex > 0 && track2.Length >= dIndex + 5) {
                    ExtractFromTrack2WithSeparator(track2, dIndex, cardNumEmpty, expiryEmpty, "=");
                }
                else {
                    // Try parsing using fixed positions
                    TryExtractFromFixedFormat(track2, cardNumEmpty, expiryEmpty);
                }
            }

            // If still unable to parse, log warning
            if (string.IsNullOrEmpty(textCardNum.Text)) {
                displayOut(0, 0, $"Warning: Unable to parse card number from Track2 data: {track2}");
            }
        }

        private void ExtractFromTrack2WithSeparator(string track2, int separatorIndex, bool extractCard, bool extractExpiry, string separator) {
            if (extractCard) {
                string pan = track2.Substring(0, separatorIndex).TrimEnd('F');
                textCardNum.Text = pan;
                displayOut(0, 0, $"Extracted card number from Track2{(separator == "D" ? "" : $" ({separator} separator)")}: {pan}");
            }

            if (extractExpiry && track2.Length >= separatorIndex + 5) {
                string expiryYYMM = track2.Substring(separatorIndex + 1, 4);
                if (Regex.IsMatch(expiryYYMM, @"^\d{4}$")) {
                    string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                    textEXP.Text = expiry;
                    displayOut(0, 0, $"Extracted expiry date from Track2{(separator == "D" ? "" : $" ({separator} separator)")}: {expiry}");
                }
            }
        }

        private void TryExtractFromFixedFormat(string track2, bool extractCard, bool extractExpiry) {
            // Try extracting PAN (assume PAN length is 16-19 digits)
            if (extractCard) {
                for (int panLength = 16; panLength <= 19; panLength++) {
                    if (track2.Length >= panLength) {
                        string possiblePan = track2.Substring(0, panLength);
                        if (Regex.IsMatch(possiblePan, @"^\d+$")) {
                            textCardNum.Text = possiblePan;
                            displayOut(0, 0, $"Extracted card number from Track2 (fixed format): {possiblePan}");

                            // Try extracting expiry date
                            if (extractExpiry && track2.Length >= panLength + 4) {
                                string expiryYYMM = track2.Substring(panLength, 4);
                                if (Regex.IsMatch(expiryYYMM, @"^\d{4}$")) {
                                    string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                                    textEXP.Text = expiry;
                                    displayOut(0, 0, $"Extracted expiry date from Track2 (fixed format): {expiry}");
                                }
                            }
                            return;
                        }
                    }
                }

                // Try special formats (UnionPay)
                TryExtractUnionPayFormat(track2, extractExpiry);
            }
        }

        private void TryExtractUnionPayFormat(string track2, bool extractExpiry) {
            if (track2.Length < 30) {
                return;
            }

            string possiblePan = track2.Substring(0, 16);
            if (possiblePan.StartsWith("62") || possiblePan.StartsWith("60")) {
                textCardNum.Text = possiblePan;
                displayOut(0, 0, $"Extracted card number from Track2 (UnionPay special format): {possiblePan}");

                if (extractExpiry && track2.Length >= 20) {
                    for (int i = 16; i <= 20 && i + 4 <= track2.Length; i++) {
                        string possibleExpiry = track2.Substring(i, 4);
                        if (Regex.IsMatch(possibleExpiry, @"^[\dD=]{4}$")) {
                            string expiryYYMM = possibleExpiry.Replace("D", "").Replace("=", "");
                            if (expiryYYMM.Length == 4 && Regex.IsMatch(expiryYYMM, @"^\d{4}$")) {
                                string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                                textEXP.Text = expiry;
                                displayOut(0, 0, $"Extracted expiry date from Track2 (UnionPay special format): {expiry}");
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void bConnect_Click(object sender, EventArgs e) {
            Logger.Info($"Attempting to connect to reader: {cbReader.Text}");
            ClearCardTextFields();
            cbPSE.Items.Clear();
            cbPSE.Text = "";

            // Connect to selected reader using hContext handle and obtain hCard handle
            if (connActive) {
                Logger.Info("Disconnecting from previous card");
                retCode = ModWinsCard64.SCardDisconnect(hCard, ModWinsCard64.SCARD_UNPOWER_CARD);
            }

            // Shared Connection
            retCode = ModWinsCard64.SCardConnect(hContext, cbReader.Text, ModWinsCard64.SCARD_SHARE_SHARED, ModWinsCard64.SCARD_PROTOCOL_T0 | ModWinsCard64.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                Logger.Error($"Failed to connect to {cbReader.Text}. Return code: {retCode}");
                displayOut(1, retCode, "");
                return;
            }
            
            Logger.Info($"Successfully connected to {cbReader.Text}");
            displayOut(0, 0, "Successful connection to " + cbReader.Text);
            
            ReadAndDisplayATR();
            connActive = true;
        }

        private void ReadAndDisplayATR() {
            byte[] atr = new byte[33];
            long atrLen = atr.Length;
            int readerLen = 0;
            int state = 0;
            
            retCode = ModWinsCard64.SCardStatus(hCard, null, ref readerLen, ref state, ref Protocol, atr, ref atrLen);

            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                displayOut(1, retCode, "Unable to read ATR");
                Logger.Error($"Failed to retrieve ATR. Return code: {retCode}");
                return;
            }

            string atrStr = BitConverter.ToString(atr, 0, (int)atrLen);
            displayOut(0, 0, "ATR: " + atrStr);
            Logger.Info($"ATR retrieved: {atrStr}");

            // Simple check if contact card
            bool isContactCard = atrLen > 0 && (atr[0] == 0x3B || atr[0] == 0x3F);
            string cardMode = isContactCard ? "contact" : "contactless";
            displayOut(0, 0, $"Card defaults to {cardMode} mode");
            Logger.Info($"Card detected in {cardMode} mode");
        }

        private void bLoadPSE_Click(object sender, EventArgs e) {
            Logger.Info("Loading PSE (Payment System Environment)");
            ClearCardTextFields();
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            labelToAID.Clear();

            // Select PSE application (1PAY.SYS.DDF01)
            if (!SelectPSEApplication()) {
                return;
            }

            // Read records from SFI 1 sequentially until return 6A 83
            ReadPSERecords();

            // Automatically select first application (if any)
            if (cbPSE.Items.Count > 0 && cbPSE.SelectedIndex == -1) {
                cbPSE.SelectedIndex = 0;
            }
            Logger.Info($"PSE loading completed. Found {cbPSE.Items.Count} application(s)");
        }

        private bool SelectPSEApplication() {
            string selectPSE = "00 A4 04 00 0E 31 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix();
            
            if (result != 0) {
                displayOut(0, 0, "Failed to select PSE application");
                Logger.Error("Failed to select PSE application");
                return false;
            }
            
            Logger.Info("PSE application selected successfully");
            return true;
        }

        private void ReadPSERecords() {
            for (int record = 1; ; record++) {
                string readSFI = $"00 B2 {record:X2} 0C 00";
                SendLen = FillBufferFromHexString(readSFI, SendBuff, 0);
                RecvLen = 0xFF;

                int result = TransmitWithAutoFix();

                // Check if "record does not exist"
                if (IsRecordNotFound()) {
                    displayOut(0, 0, $"Record {record} does not exist, finished reading AIDs");
                    Logger.Info($"PSE Record {record} does not exist, finished reading AIDs");
                    break;
                }

                // Check if successful
                if (result != 0 || !IsSuccessResponse()) {
                    displayOut(0, 0, $"Record {record} read failed, stopping");
                    Logger.Warning($"Failed to read PSE Record {record}");
                    break;
                }

                displayOut(0, 0, $"Parsing AID information from Record {record}");
                Logger.Info($"Parsing AID information from Record {record}");
                ParseSFIRecord(RecvBuff, RecvLen - 2);
            }
        }

        private bool IsRecordNotFound() {
            return RecvLen == 2 && RecvBuff[0] == 0x6A && RecvBuff[1] == 0x83;
        }

        private void bLoadPPSE_Click(object sender, EventArgs e) {
            Logger.Info("Loading PPSE (Proximity Payment System Environment)");
            ClearCardTextFields();
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            labelToAID.Clear();

            // Select PPSE application
            if (!SelectPPSEApplication()) {
                return;
            }

            // Find all Application Templates (61) from returned FCI Template
            int appCount = ParsePPSEApplications();

            // Automatically select first
            if (cbPSE.Items.Count > 0 && cbPSE.SelectedIndex == -1) {
                cbPSE.SelectedIndex = 0;
            }
            Logger.Info($"PPSE loading completed. Found {appCount} application(s)");
        }

        private bool SelectPPSEApplication() {
            string selectPPSE = "00 A4 04 00 0E 32 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix();
            
            if (result != 0 || !IsSuccessResponse()) {
                displayOut(0, 0, "Failed to select PPSE application");
                Logger.Error("Failed to select PPSE application");
                return false;
            }
            
            Logger.Info("PPSE application selected successfully");
            return true;
        }

        private int ParsePPSEApplications() {
            int index = 0;
            int appCount = 0;
            
            while (index < RecvLen - 2) {
                if (RecvBuff[index] != 0x61) {
                    index++;
                    continue;
                }

                int len = RecvBuff[index + 1];
                int start = index + 2;
                int end = start + len;
                
                if (end > RecvLen - 2) {
                    break;
                }

                var (aid, label) = ParseApplicationTemplate(start, end);

                if (!string.IsNullOrEmpty(aid)) {
                    if (string.IsNullOrEmpty(label)) {
                        label = "App_" + aid.Substring(Math.Max(0, aid.Length - 4));
                    }
                    
                    if (!cbPSE.Items.Contains(label)) {
                        AddApplicationToList(aid, label);
                        appCount++;
                    }
                }

                index = end;
            }
            
            return appCount;
        }

        private (string aid, string label) ParseApplicationTemplate(int start, int end) {
            string currentAID = "";
            string label = "";
            int subIndex = start;

            while (subIndex < end) {
                byte tag = RecvBuff[subIndex++];
                if (subIndex >= end) break;
                
                int tagLen = RecvBuff[subIndex++];
                if (subIndex + tagLen > end) break;
                
                byte[] value = new byte[tagLen];
                Array.Copy(RecvBuff, subIndex, value, 0, tagLen);
                subIndex += tagLen;

                if (tag == 0x4F) {
                    currentAID = string.Join(" ", value.Select(b => b.ToString("X2")));
                    displayOut(0, 0, "AID: " + currentAID);
                }
                else if (tag == 0x50) {
                    label = Encoding.ASCII.GetString(value).Trim();
                    displayOut(0, 0, "Application Label: " + label);
                }
            }

            return (currentAID, label);
        }

        private void AddApplicationToList(string aid, string label) {
            int aidIndex = cbPSE.Items.Count + 1;
            string itemName = $"{aidIndex}. {label}";
            cbPSE.Items.Add(itemName);
            labelToAID[itemName] = aid;
            Logger.Info($"Found application: {label} with AID: {aid}");
        }

        private void bReset_Click(object sender, EventArgs e) {
            Logger.Info("Resetting application and releasing card reader resources");
            
            if (connActive) {
                retCode = ModWinsCard64.SCardDisconnect(hCard, ModWinsCard64.SCARD_UNPOWER_CARD);
                Logger.Info("Disconnected from card");
            }
            
            ClearAllFields();
            bInit.Enabled = true;
            retCode = ModWinsCard64.SCardReleaseContext(hContext);
            Logger.Info("Context released. Application reset completed");
        }

        private void ClearAllFields() {
            cbReader.Items.Clear();
            cbReader.Text = "";
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            richTextBoxLogs.Clear();
            ClearCardTextFields();
        }

        private void clearInterface() {
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            richTextBoxLogs.Clear();
            ClearCardTextFields();
        }

        private int TransmitWithAutoFix() {
            int result = SendAPDUandDisplay();

            // Case 1: SW = 6C XX, indicates Le mismatch, need to resend
            if (RecvLen == 2 && RecvBuff[0] == 0x6C) {
                SendBuff[SendLen - 1] = RecvBuff[1];
                RecvLen = RecvBuff[1] + 2;
                return SendAPDUandDisplay();
            }

            // Case 2: SW = 67 00, indicates missing Le, try supplementing with 0xFF and resend
            if (RecvLen == 2 && RecvBuff[0] == 0x67 && RecvBuff[1] == 0x00) {
                SendBuff[SendLen - 1] = 0xFF;
                RecvLen = 0xFF;
                return SendAPDUandDisplay();
            }

            // Case 3: SW = 61 XX, need GET RESPONSE
            if (RecvLen == 2 && RecvBuff[0] == 0x61) {
                byte le = RecvBuff[1];
                SendLen = FillBufferFromHexString($"00 C0 00 00 {le:X2}", SendBuff, 0);
                RecvLen = le + 2;
                return SendAPDUandDisplay();
            }

            return result;
        }

        public int FillBufferFromHexString(string hexString, byte[] buffer, int startIndex) {
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

            for (int i = 0; i < byteCount; i++) {
                if (!byte.TryParse(hexValues[i], System.Globalization.NumberStyles.HexNumber, null, out byte result))
                    throw new FormatException($"Unable to parse '{hexValues[i]}' as hexadecimal byte");
                buffer[startIndex + i] = result;
            }

            return byteCount;
        }
    }
}