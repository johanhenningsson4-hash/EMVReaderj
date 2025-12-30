# EMV Card Reader

A Windows application for reading and analyzing EMV (Europay, Mastercard, and Visa) chip cards using PC/SC-compatible smart card readers.

## Overview

EMVReader is a comprehensive tool for interfacing with EMV payment cards through smart card readers. It supports both contact and contactless card reading, provides detailed transaction data extraction, and includes robust logging capabilities for debugging and analysis.

## Features

### Card Reading Capabilities
- **Multiple Protocol Support**: Supports both T0 and T1 protocols
- **PSE/PPSE Support**: Reads Payment System Environment (PSE) and Proximity Payment System Environment (PPSE)
- **Application Selection**: Automatically detects and lists available card applications
- **EMV Data Extraction**: Retrieves card number (PAN), expiration date, cardholder name, and Track 2 data
- **Contact and Contactless**: Works with both contact and contactless smart card readers

### Advanced EMV Operations
- **GET PROCESSING OPTIONS (GPO)**: Automatically constructs PDOL (Processing Data Object List) data
- **AFL Parsing**: Reads Application File Locator records to extract card data
- **TLV Parsing**: Comprehensive Tag-Length-Value parsing for EMV data structures
- **Automatic Fallback**: Tries common record locations when standard methods fail
- **Track 2 Analysis**: Extracts missing information from Track 2 equivalent data

### Logging System
- **File-Based Logging**: All operations logged to rotating log files
- **Multiple Severity Levels**: Info, Warning, Error, and Debug levels
- **Automatic Log Rotation**: Logs rotate when reaching 5MB size limit
- **Log Management**: Maintains up to 10 log files automatically
- **Detailed APDU Logging**: Captures all card commands and responses

### User Interface
- **Real-Time Display**: Live display of APDU commands and responses
- **Card Data Display**: Clear presentation of extracted card information
- **Reader Selection**: Dropdown list of available smart card readers
- **Application Selection**: List of detected card applications

## System Requirements

### Hardware
- PC/SC-compatible smart card reader (contact or contactless)
- Windows operating system (Windows 7 or later recommended)

### Software
- .NET Framework 4.7.2 or later
- Visual Studio 2012 or later (for development)

### Compatible Readers
- ACS readers (ACR series)
- Most PC/SC-compliant smart card readers
- Both USB and built-in readers supported

## Installation

### Binary Installation
1. Download the latest release from the releases page
2. Extract the ZIP file to a folder
3. Ensure your smart card reader is connected and drivers installed
4. Run `EMVReader.exe`

### Building from Source
1. Clone the repository:
   ```bash
   git clone https://github.com/johanhenningsson4-hash/EMVReaderj.git
   cd EMVReaderj
   ```

2. Open `EMVReader.sln` in Visual Studio

3. Add `Logger.cs` to the project:
   - Right-click on the project in Solution Explorer
   - Select "Add" > "Existing Item"
   - Select `Logger.cs`

4. Build the solution:
   - Press F6 or select "Build" > "Build Solution"
   - The executable will be in `bin\Debug` or `bin\Release`

## Usage

### Basic Card Reading

1. **Initialize Reader**
   - Click "Init" to detect connected smart card readers
   - Select your reader from the dropdown list

2. **Connect to Card**
   - Place card on/in reader
   - Click "Connect" to establish connection
   - ATR (Answer To Reset) will be displayed

3. **Load Applications**
   - Click "Load PSE" for contact cards
   - Click "Load PPSE" for contactless cards
   - Available applications will be listed

4. **Read Card Data**
   - Select an application from the dropdown
   - Click "Read Application"
   - Card data will be extracted and displayed

### Understanding the Output

#### Log Display
The main text area shows:
- `<` prefix: Commands sent to card (APDU requests)
- `>` prefix: Responses from card (APDU responses)
- General messages: Operation status and extracted data

#### Card Information Fields
- **Card Number**: Primary Account Number (PAN)
- **Expiration Date**: Card expiry date (YYYY-MM format)
- **Holder Name**: Cardholder name (if available)
- **Track 2 Data**: Magnetic stripe equivalent data

### Logging

Logs are automatically saved to the `Logs` directory in the application folder.

#### Log File Format
```
[2024-12-30 18:45:32.123] [INFO] EMV Reader application started
[2024-12-30 18:45:35.456] [INFO] Found reader: ACS ACR122U PICC Interface
[2024-12-30 18:45:40.789] [DEBUG] APDU Sent: < 00 A4 04 00...
[2024-12-30 18:45:40.890] [DEBUG] APDU Response: > 90 00
```

#### Log Levels
- **INFO**: General operation information (connections, successful operations)
- **WARNING**: Non-critical issues (fallback operations, optional data not found)
- **ERROR**: Errors that prevent operation (connection failures, command errors)
- **DEBUG**: Detailed APDU communication for troubleshooting

#### Managing Logs
- Logs automatically rotate when reaching 5MB
- Maximum 10 log files are retained
- Older logs are automatically deleted
- Log files are named: `EMVReader_YYYYMMDD.log`

## EMV Standards Supported

### Card Specifications
- EMV 4.3 specifications
- ISO/IEC 7816 (contact cards)
- ISO/IEC 14443 (contactless cards)

### Supported Data Objects
- **5A**: Primary Account Number (PAN)
- **5F24**: Application Expiration Date
- **5F20**: Cardholder Name
- **57**: Track 2 Equivalent Data
- **9F6B**: Track 2 Data
- **4F**: Application Identifier (AID)
- **50**: Application Label
- **9F38**: PDOL
- **94**: AFL (Application File Locator)

## Troubleshooting

### Common Issues

#### Reader Not Detected
- Ensure reader drivers are installed
- Check USB connection
- Try "Reset" button and "Init" again
- Verify reader appears in Device Manager

#### Card Not Responding
- Ensure card is properly seated/positioned
- Try removing and reinserting card
- Check if card is supported EMV card
- Verify card is not damaged

#### No Applications Found
- Some cards may not support PSE/PPSE
- Try both "Load PSE" and "Load PPSE"
- Card may require direct AID selection
- Check logs for detailed error messages

#### Incomplete Card Data
- Some data fields are optional in EMV
- Check Track 2 data for missing information
- Logs show which records were successfully read
- Some banks don't store cardholder name on chip

### Debug Mode

For detailed troubleshooting:
1. Check the `Logs` folder for detailed operation logs
2. Look for ERROR and WARNING level messages
3. Review APDU commands and responses
4. Verify card compatibility with EMV standards

## Security Considerations

### Data Handling
- This application reads card data but does NOT store sensitive information
- Card PINs are never requested or transmitted
- Application is read-only and cannot modify card data
- Use responsibly and only on cards you own or have permission to read

### Compliance
- Tool is for educational and testing purposes
- Comply with local regulations regarding card data
- Do not use for unauthorized access to payment cards
- PCI-DSS compliance required for commercial use

## Architecture

### Project Structure
```
EMVReaderj/
??? EMVReader.cs          # Main form and EMV logic
??? EMVReader.Designer.cs # Form designer code
??? Logger.cs             # Logging system
??? ModWinsCard64.cs      # PC/SC wrapper for Windows
??? Program.cs            # Application entry point
??? Properties/           # Application properties and resources
??? Logs/                 # Log files directory (created at runtime)
```

### Key Components

#### EMVReader Class
- Main application logic
- APDU command handling
- EMV data parsing
- UI management

#### Logger Class
- Static logging utility
- File rotation management
- Multiple severity levels
- Thread-safe operations

#### ModWinsCard64
- Windows Smart Card API wrapper
- PC/SC function imports
- Low-level card communication

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Development Guidelines
- Follow existing code style
- Add logging to new features
- Update README for new functionality
- Test with multiple card types if possible

## License

This project is licensed under the terms specified in the LICENSE file.

Copyright (C) Advanced Card Systems Ltd

## Acknowledgments

- Advanced Card Systems Ltd for the original codebase
- EMVCo for EMV specifications
- PC/SC Workgroup for smart card standards

## Support

For issues, questions, or contributions:
- GitHub Issues: https://github.com/johanhenningsson4-hash/EMVReaderj/issues
- Repository: https://github.com/johanhenningsson4-hash/EMVReaderj

## Version History

### Current Version
- Added comprehensive logging system with file rotation
- Enhanced EMV data extraction with Track 2 fallback
- Improved PPSE support with application indexing
- Added automatic PDOL construction for GPO
- Enhanced error handling and user feedback

### Future Enhancements
- Support for additional EMV data elements
- Export functionality for card data
- Batch card reading capabilities
- Enhanced contactless card support
- Configuration file for logging settings

## References

- EMV Specifications: https://www.emvco.com/specifications/
- ISO/IEC 7816 Standard: Smart card specifications
- PC/SC Workgroup: https://pcscworkgroup.com/
- Advanced Card Systems: https://www.acs.com.hk/
