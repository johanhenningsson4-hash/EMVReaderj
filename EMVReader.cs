/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
'  
'  Author :         Eternal TUTU
'
'  Module :         EMVReader.cs (UI Layer - Refactored)
'   
'  Date   :         June 23, 2008
'  Refactored:      January 2025
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
        #region Private Fields
        private EMVCardReader emvReader;
        #endregion

        #region Form Initialization
        public MainEMVReaderBin()
        {
            InitializeComponent();
            Logger.Initialize();
            Logger.Info("EMV Reader application started");
            
            InitializeEMVReader();
        }

        private void InitializeEMVReader()
        {
            emvReader = new EMVCardReader();
            
            // Subscribe to events
            emvReader.OnMessage += OnEMVMessage;
            emvReader.OnAPDUSent += OnAPDUSent;
            emvReader.OnAPDUReceived += OnAPDUReceived;
            emvReader.OnError += OnEMVError;
            emvReader.OnCardDataExtracted += OnCardDataExtracted;
        }
        #endregion

        #region Event Handlers from EMVCardReader
        private void OnEMVMessage(object sender, string message)
        {
            DisplayMessage(message);
        }

        private void OnAPDUSent(object sender, APDUEventArgs e)
        {
            DisplayMessage($"< {e.APDU}");
        }

        private void OnAPDUReceived(object sender, APDUEventArgs e)
        {
            DisplayMessage($"> {e.APDU}");
        }

        private void OnEMVError(object sender, ErrorEventArgs e)
        {
            DisplayMessage($"Error: {e.Error}");
        }

        private void OnCardDataExtracted(object sender, CardDataEventArgs e)
        {
            UpdateCardDataFields(e.Data);
        }
        #endregion

        #region UI Helper Methods
        private void DisplayMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(DisplayMessage), message);
                return;
            }

            richTextBoxLogs.Select(richTextBoxLogs.Text.Length, 0);
            richTextBoxLogs.SelectedText = message + "\r\n";
            richTextBoxLogs.ScrollToCaret();
        }

        private void UpdateCardDataFields(CardData cardData)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<CardData>(UpdateCardDataFields), cardData);
                return;
            }

            textCardNum.Text = cardData.PAN;
            textEXP.Text = cardData.ExpiryDate;
            textHolder.Text = cardData.CardholderName;
            textTrack.Text = cardData.Track2Data;
        }

        private void ClearCardTextFields()
        {
            textCardNum.Text = "";
            textEXP.Text = "";
            textHolder.Text = "";
            textTrack.Text = "";
        }

        private void ClearAllFields()
        {
            cbReader.Items.Clear();
            cbReader.Text = "";
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            richTextBoxLogs.Clear();
            ClearCardTextFields();
        }

        private void EnableButtons()
        {
            bInit.Enabled = false;
            bConnect.Enabled = true;
            bLoadPSE.Enabled = true;
            bLoadPPSE.Enabled = true;
            bReadApp.Enabled = true;
            bReset.Enabled = true;
            bClear.Enabled = true;
        }

        private void PopulateReaderList()
        {
            cbReader.Items.Clear();
            foreach (string reader in emvReader.AvailableReaders)
            {
                cbReader.Items.Add(reader);
            }

            if (cbReader.Items.Count > 0)
                cbReader.SelectedIndex = 0;
        }

        private void PopulateApplicationList()
        {
            cbPSE.Items.Clear();
            foreach (var app in emvReader.Applications)
            {
                cbPSE.Items.Add(app.Key);
            }

            if (cbPSE.Items.Count > 0 && cbPSE.SelectedIndex == -1)
                cbPSE.SelectedIndex = 0;
        }
        #endregion

        #region Button Event Handlers
        private void bInit_Click(object sender, EventArgs e)
        {
            Logger.Info("Initializing smart card readers");
            
            if (emvReader.Initialize())
            {
                PopulateReaderList();
                EnableButtons();
                DisplayMessage($"Initialization successful. Found {emvReader.AvailableReaders.Count} reader(s)");
            }
            else
            {
                DisplayMessage("Initialization failed. Check logs for details.");
            }
        }

        private void bConnect_Click(object sender, EventArgs e)
        {
            if (cbReader.SelectedItem == null)
            {
                DisplayMessage("Please select a card reader first");
                return;
            }

            Logger.Info($"Attempting to connect to reader: {cbReader.Text}");
            ClearCardTextFields();
            cbPSE.Items.Clear();
            cbPSE.Text = "";

            string selectedReader = cbReader.Text;
            if (emvReader.ConnectToReader(selectedReader))
            {
                DisplayMessage($"Successfully connected to {selectedReader}");
            }
            else
            {
                DisplayMessage($"Failed to connect to {selectedReader}");
            }
        }

        private void bLoadPSE_Click(object sender, EventArgs e)
        {
            if (!emvReader.IsConnected)
            {
                DisplayMessage("Please connect to a card first");
                return;
            }

            Logger.Info("Loading PSE applications");
            ClearCardTextFields();
            
            if (emvReader.LoadPSEApplications())
            {
                PopulateApplicationList();
                DisplayMessage($"PSE loading completed. Found {emvReader.Applications.Count} applications");
            }
            else
            {
                DisplayMessage("Failed to load PSE applications");
            }
        }

        private void bLoadPPSE_Click(object sender, EventArgs e)
        {
            if (!emvReader.IsConnected)
            {
                DisplayMessage("Please connect to a card first");
                return;
            }

            Logger.Info("Loading PPSE applications");
            ClearCardTextFields();
            
            if (emvReader.LoadPPSEApplications())
            {
                PopulateApplicationList();
                DisplayMessage($"PPSE loading completed. Found {emvReader.Applications.Count} applications");
            }
            else
            {
                DisplayMessage("Failed to load PPSE applications");
            }
        }

        private void bReadApp_Click(object sender, EventArgs e)
        {
            if (!emvReader.IsConnected)
            {
                DisplayMessage("Please connect to a card first");
                return;
            }

            if (cbPSE.SelectedItem == null)
            {
                DisplayMessage("Please select an application first");
                return;
            }

            Logger.Info("Starting EMV application read operation");
            string selectedApplication = cbPSE.Text.Trim();
            
            var cardData = emvReader.ReadApplication(selectedApplication);
            if (cardData != null && !cardData.IsEmpty)
            {
                DisplayMessage("Card data extraction completed successfully");
            }
            else
            {
                DisplayMessage("Failed to extract card data or card data is empty");
            }
        }

        private void bReset_Click(object sender, EventArgs e)
        {
            Logger.Info("Resetting application and releasing card reader resources");
            
            emvReader?.Shutdown();
            ClearAllFields();
            bInit.Enabled = true;
            
            // Reinitialize the EMV reader for next use
            InitializeEMVReader();
            
            DisplayMessage("Application reset completed");
            Logger.Info("Application reset completed");
        }

        private void bClear_Click(object sender, EventArgs e)
        {
            richTextBoxLogs.Clear();
        }

        private void bQuit_Click(object sender, EventArgs e)
        {
            Logger.Info("Application shutting down");
            
            emvReader?.Shutdown();
            Logger.Info("Resources released. Application terminated");
            
            System.Environment.Exit(0);
        }
        #endregion

        #region Form Events
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            emvReader?.Shutdown();
            base.OnFormClosing(e);
        }
        #endregion
    }
}