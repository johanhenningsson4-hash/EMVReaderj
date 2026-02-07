# EMV Card Reader - Testing Guide

## Overview

This document provides comprehensive testing instructions for the EMV Card Reader project, including unit tests, integration tests, and mock testing scenarios.

## Test Structure

```
EMVCard.Tests/
??? UnitTests/
?   ??? LoggerTests.cs           - Logger functionality tests
?   ??? BufferPoolTests.cs       - Memory pool tests
?   ??? ConfigurationTests.cs    - Configuration system tests
?   ??? TLVParsingTests.cs       - TLV parsing logic tests
?   ??? Track2ParsingTests.cs    - Track 2 data parsing tests
??? IntegrationTests/
?   ??? CardReaderIntegrationTests.cs - End-to-end workflow tests
??? MockTests/
?   ??? MockCardReaderTests.cs   - Hardware-independent tests
??? TestUtilities/
?   ??? MockObjects.cs           - Mock implementations
?   ??? TestData.cs              - Static test data
?   ??? TestHelpers.cs           - Test helper methods
??? TestData/
    ??? SampleCards.json         - Sample card data
    ??? TestAPDUResponses.xml    - APDU response samples
```

## Prerequisites

### Software Requirements
- Visual Studio 2012 or later
- .NET Framework 4.7.2
- MSTest Test Framework 2.2.10
- MSTest Test Adapter 2.2.10

### Test Project Setup
1. **Add Test Project to Solution**:
   ```
   Right-click Solution ? Add ? Existing Project ? EMVCard.Tests.csproj
   ```

2. **Restore NuGet Packages**:
   ```
   Right-click EMVCard.Tests ? Manage NuGet Packages ? Restore
   ```

3. **Build Test Project**:
   ```
   Build ? Build EMVCard.Tests
   ```

## Running Tests

### Visual Studio Test Explorer
1. Open **Test Explorer**: `Test ? Test Explorer`
2. Build the solution to discover tests
3. Run all tests: `Run All Tests in View`
4. Run specific test categories:
   - Unit Tests: Filter by `TestCategory:Unit`
   - Integration Tests: Filter by `TestCategory:Integration`
   - Mock Tests: Filter by `TestCategory:Mock`

### Command Line Testing
```powershell
# Run all tests
dotnet test EMVCard.Tests\EMVCard.Tests.csproj

# Run with detailed output
dotnet test EMVCard.Tests\EMVCard.Tests.csproj --logger "console;verbosity=detailed"

# Run specific test category
dotnet test EMVCard.Tests\EMVCard.Tests.csproj --filter "TestCategory=Unit"

# Generate code coverage report (requires additional tools)
dotnet test EMVCard.Tests\EMVCard.Tests.csproj --collect:"XPlat Code Coverage"
```

### MSTest Command Line
```cmd
# Using VSTest.Console.exe
VSTest.Console.exe EMVCard.Tests\bin\Debug\EMVCard.Tests.dll /Settings:test.runsettings

# With specific test filter
VSTest.Console.exe EMVCard.Tests\bin\Debug\EMVCard.Tests.dll /TestCaseFilter:"TestCategory=Unit"
```

## Test Categories

### 1. Unit Tests

#### Logger Tests (`LoggerTests.cs`)
**Purpose**: Test logging functionality
**Key Test Cases**:
- ? Log file creation and rotation
- ? Multiple severity levels
- ? Thread safety
- ? Configuration changes
- ? Error handling

**Run Command**:
```cmd
dotnet test --filter "ClassName~LoggerTests"
```

#### BufferPool Tests (`BufferPoolTests.cs`)
**Purpose**: Test memory pool efficiency
**Key Test Cases**:
- ? Buffer rental and return
- ? Size optimization
- ? Memory clearing
- ? Concurrent access
- ? Pool limits

**Run Command**:
```cmd
dotnet test --filter "ClassName~BufferPoolTests"
```

#### TLV Parsing Tests (`TLVParsingTests.cs`)
**Purpose**: Test EMV data parsing
**Key Test Cases**:
- ? Hex string conversion
- ? TLV structure parsing
- ? Track 2 data extraction
- ? Error handling
- ? Edge cases

**Run Command**:
```cmd
dotnet test --filter "ClassName~TLVParsingTests"
```

### 2. Integration Tests

#### Card Reader Integration (`CardReaderIntegrationTests.cs`)
**Purpose**: Test complete workflows
**Key Test Cases**:
- ? End-to-end card reading
- ? Multiple card types
- ? Error scenarios
- ? Concurrent operations
- ? Data extraction

**Run Command**:
```cmd
dotnet test --filter "TestCategory=Integration"
```

### 3. Mock Tests

**Purpose**: Test without hardware dependencies
**Features**:
- Mock card readers
- Simulated card responses
- Various card types (Visa, Mastercard, UnionPay)
- Error condition simulation

**Run Command**:
```cmd
dotnet test --filter "TestCategory=Mock"
```

## Test Data

### Sample Cards
The test suite includes sample data for:
- **Visa Credit**: `4532123456789012`
- **Mastercard**: `5555666677778888`
- **UnionPay**: `6231871800000762`
- **American Express**: `371449635398431`

### APDU Responses
Pre-configured responses for:
- PSE/PPSE selection
- Application selection
- GPO (Get Processing Options)
- Record reading
- Error conditions

### TLV Test Data
Sample TLV structures for:
- Simple tags (PAN, Expiry Date)
- Two-byte tags (PDOL)
- Long-form lengths
- Nested structures

## Performance Testing

### Buffer Pool Performance
```csharp
[TestMethod]
public void BufferPool_Performance_RentReturn()
{
    const int iterations = 10000;
    var stopwatch = Stopwatch.StartNew();
    
    for (int i = 0; i < iterations; i++)
    {
        var buffer = BufferPool.Rent(256);
        BufferPool.Return(buffer);
    }
    
    stopwatch.Stop();
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000); // Should complete in < 1 second
}
```

### String Processing Performance
```csharp
[TestMethod]
public void HexString_Performance_LargeData()
{
    string largeHex = string.Join(" ", Enumerable.Range(0, 1000).Select(i => "FF"));
    byte[] buffer = new byte[1000];
    
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 100; i++)
    {
        emvReader.FillBufferFromHexString(largeHex, buffer, 0);
    }
    stopwatch.Stop();
    
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500); // Should be fast
}
```

## Continuous Integration

### GitHub Actions Configuration
```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET Framework
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 4.7.2
        
    - name: Restore packages
      run: nuget restore
      
    - name: Build
      run: msbuild /p:Configuration=Release
      
    - name: Test
      run: dotnet test EMVCard.Tests\EMVCard.Tests.csproj --logger "trx;LogFileName=test_results.trx"
      
    - name: Publish test results
      uses: dorny/test-reporter@v1
      if: success() || failure()
      with:
        name: Test Results
        path: test_results.trx
        reporter: dotnet-trx
```

## Test Coverage

### Expected Coverage Targets
- **Unit Tests**: > 85%
- **Integration Tests**: > 70%
- **Overall**: > 80%

### Measuring Coverage
1. Install coverage tool:
```cmd
dotnet tool install -g dotnet-coverage
```

2. Run with coverage:
```cmd
dotnet coverage collect "dotnet test" -f xml -o coverage.xml
```

3. Generate HTML report:
```cmd
dotnet tool install -g reportgenerator
reportgenerator -reports:coverage.xml -targetdir:coverage-report
```

## Debugging Tests

### Test Settings File (`test.runsettings`)
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <MaxCpuCount>1</MaxCpuCount>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
  </RunConfiguration>
  
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Code Coverage" uri="datacollector://Microsoft/CodeCoverage/2.0">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Include>
                <ModulePath>.*EMVCard\.exe$</ModulePath>
              </Include>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### Debugging Individual Tests
1. Set breakpoints in test methods
2. Right-click test in Test Explorer
3. Select "Debug Selected Tests"
4. Use standard debugging tools

## Best Practices

### Writing New Tests

1. **Follow AAA Pattern**:
```csharp
[TestMethod]
public void Method_Scenario_ExpectedResult()
{
    // Arrange
    var input = "test input";
    var expected = "expected output";
    
    // Act
    var result = MethodUnderTest(input);
    
    // Assert
    Assert.AreEqual(expected, result);
}
```

2. **Use Descriptive Names**:
- ? `Logger_ThreadSafety_ConcurrentLogging`
- ? `TestLogger1`

3. **Test Edge Cases**:
- Null inputs
- Empty strings
- Boundary values
- Error conditions

4. **Keep Tests Independent**:
- No dependencies between tests
- Clean up resources in `[TestCleanup]`
- Use fresh test data

### Mock Object Guidelines

1. **Keep Mocks Simple**:
```csharp
var mockReader = MockScenarios.CreateReaderWithVisaCard();
mockReader.Connect();
```

2. **Use Realistic Test Data**:
```csharp
var testCard = MockCard.CreateVisaCard(); // Uses real Visa test numbers
```

3. **Simulate Error Conditions**:
```csharp
mockReader.QueueResponse("00A404000799999999", "6A82"); // File not found
```

## Troubleshooting

### Common Issues

1. **Tests Not Discovered**:
   - Rebuild solution
   - Check test project references
   - Verify MSTest packages installed

2. **File Access Errors**:
   - Check test directory permissions
   - Ensure cleanup in `[TestCleanup]`
   - Use unique temporary directories

3. **Threading Issues**:
   - Use proper synchronization in tests
   - Avoid shared static state
   - Use `Task.WaitAll()` for async operations

4. **Memory Leaks in Tests**:
   - Dispose of resources properly
   - Return buffers to pool
   - Clear large test data

### Performance Issues

1. **Slow Tests**:
   - Reduce iteration counts for CI
   - Use `[Timeout]` attributes
   - Profile test execution

2. **Memory Usage**:
   - Monitor buffer pool stats
   - Check for unreturned buffers
   - Use memory profilers

## Reporting

### Test Results
Tests generate detailed reports including:
- Pass/fail status
- Execution time
- Code coverage
- Performance metrics

### CI Integration
- Automatic test execution on commits
- Test result publishing
- Coverage report generation
- Failure notifications

---

## Quick Start

1. **Build and run all tests**:
```cmd
dotnet build
dotnet test EMVCard.Tests\EMVCard.Tests.csproj
```

2. **Run specific test category**:
```cmd
dotnet test --filter "TestCategory=Unit"
```

3. **Debug failing test**:
- Open Test Explorer
- Right-click failing test
- Select "Debug Selected Tests"

For detailed documentation of specific test scenarios and advanced testing techniques, see the individual test class documentation.