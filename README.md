# Patient Equipment Processor - Development Information

## IDE and Tools Used

- **IDE**: Cursor (AI-powered code editor)
- **.NET SDK**: .NET 8.0
- **Package Manager**: NuGet
- **Key Packages**:
  - Microsoft.Extensions.Configuration (v8.0.0) - For configuration management
  - Microsoft.Extensions.Configuration.Json (v8.0.0) - For JSON configuration files
  - Microsoft.Extensions.Logging (v8.0.0) - For structured logging
  - Microsoft.Extensions.Logging.Console (v8.0.0) - For console logging output
  - System.Text.Json - Built-in .NET library for JSON serialization and parsing JSON-wrapped physician notes
- **Testing Framework**: xUnit (v2.5.3)

## AI Development Tools

- **Cursor AI**: Used in the refactoring process for:
  - Variable and method renaming
  - Commenting and logging implementation
  - Setting up configuration file and reading settings
  - Creating unit tests
  - Helping create this README.md file

## Assumptions

1. **File Format**: The application assumes physician notes are either:
   - Plain text files with key-value pairs (e.g., "Patient Name: John Doe")
   - JSON files with a "data" property containing the text content

2. **Required Fields**: The application expects certain fields in the physician notes:
   - Patient Name
   - DOB (Date of Birth)
   - Diagnosis
   - Prescription or Recommendation (for device type extraction)
   - Ordering Physician
   - Usage (for oxygen tank devices)

3. **Device Types**: The application currently supports:
   - CPAP
   - Oxygen Tank
   - Wheelchair

## Limitations

1. **Device Type Detection**: Currently uses simple string matching which may not handle all variations or edge cases.

2. **Error Handling**: Exception handling is implemented with proper logging. The application could benefit from:
   - Retry logic for API calls
   - More granular error messages
   - Logging to a file or external service (currently logs to console)

3. **Validation**: Limited validation of extracted data (e.g., date format validation, required field checks).

4. **Configuration**: ✅ File paths and API endpoints are now configurable via `appsettings.json` and command-line arguments.

5. **Oxygen Details**: Liter extraction uses regex which may not handle all measurement formats.

6. **Usage Extraction**: Only handles "sleep" and "exertion" keywords - may miss other usage patterns.

## Future Improvements

1. **Configuration Management**: 
   - Consider adding environment variable support
   - Consider adding different configuration files for different environments (dev, prod)

2. **Enhanced Logging**:
   - Consider adding file-based logging
   - Consider adding request/response logging for API calls

3. **Data Validation**:
   - Validate date formats
   - Ensure required fields are present
   - Validate device type values

4. **Error Recovery**:
   - Implement retry logic with exponential backoff for API calls
   - Add circuit breaker pattern for API failures
   - Queue failed requests for retry

5. **Testing**:
   - Consider adding more comprehensive unit tests for all extraction methods
   - Consider adding integration tests for file parsing
   - Consider adding mock API tests

6. **Code Organization**:
   - Consider extracting extraction logic into separate service classes
   - Implement dependency injection for better testability
   - Add interfaces for better abstraction

7. **Documentation**:
   - Add XML documentation comments to all public methods
   - Create API documentation

8. **LLM Integration** (Stretch Goal):
   - Replace manual extraction with LLM-based extraction for better accuracy
   - Support more complex note formats

9. **Additional Device Types**:
    - Support more DME device types
    - Handle device-specific attributes more flexibly

## Instructions to Run the Project

### Prerequisites

- .NET 8.0 SDK installed
- A physician note file (e.g., `physician_note1.txt` or `physician_note2.txt`) (examples provided in project)

### Configuration

The application uses `appsettings.json` for configuration. You can modify the following settings:

```json
{
  "PhysicianNotePath": "physician_note1.txt",
  "ApiUrl": "https://alert-api.com/DrExtract"
}
```

- **PhysicianNotePath**: Path to the physician note file to process
- **ApiUrl**: The API endpoint URL where the extracted data will be sent

### Building the Project

```bash
dotnet build
```

### Running Tests

To run the unit tests:

```bash
dotnet test PatientEquipmentProcessor.Tests/PatientEquipmentProcessor.Tests.csproj
```

### Running the Project

1. **Using configuration file** (recommended):
   - Edit `appsettings.json` to set your desired file path and API URL
   - Run the application:
     ```bash
     dotnet run
     ```

2. **Override file path via command line**:
   - You can override the file path from `appsettings.json` by passing it as a command-line argument:
     ```bash
     dotnet run physician_note2.txt
     ```
   - The API URL will still be read from `appsettings.json`

3. **Output**: The application will:
   - Parse the physician note file
   - Extract structured data
   - Log progress and extracted information to the console
   - Send the data to the API endpoint specified in `appsettings.json`

### Testing with Different Files

You can test with different physician note files in two ways:

1. **Update `appsettings.json`**:
   ```json
   {
     "PhysicianNotePath": "physician_note2.txt",
     "ApiUrl": "https://alert-api.com/DrExtract"
   }
   ```

2. **Use command-line argument**:
   ```bash
   dotnet run physician_note2.txt
   ```

Supported file formats:
- `physician_note1.txt` - Plain text format
- `physician_note2.txt` - JSON-wrapped format

### Example Output

For `physician_note1.txt`, the output should be:
```json
{
  "device": "Oxygen Tank",
  "ordering_provider": "Dr. Cuddy",
  "patient_name": "Harold Finch",
  "dob": "04/12/1952",
  "diagnosis": "COPD",
  "liters": "2 L",
  "usage": "sleep and exertion"
}
```

