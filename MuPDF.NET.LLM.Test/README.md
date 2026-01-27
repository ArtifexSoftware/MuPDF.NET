# MuPDF.NET.LLM.Test

Unit tests for the MuPDF.NET.LLM project.

## Test Structure

The test project follows the same structure as `MuPDF.NET.Test` and uses NUnit as the testing framework.

## Test Classes

- **MuPDFLLMTest**: Tests for the main `MuPDFLLM` static class
  - Version information
  - Document conversion methods (ToMarkdown, ToJson, ToText)
  - LlamaIndex reader creation
  - Error handling

- **PDFMarkdownReaderTest**: Tests for the `PDFMarkdownReader` class
  - Constructor tests
  - LoadData method with various parameters
  - MetaFilter functionality
  - Error handling

- **UtilsTest**: Tests for utility functions
  - White character detection
  - Bullet character detection
  - Constants validation

- **IdentifyHeadersTest**: Tests for header identification
  - Constructor with various parameters
  - Header ID generation
  - Error handling

- **VersionInfoTest**: Tests for version information
  - Version string validation
  - Minimum MuPDF version validation

## Running Tests

Tests can be run using:
- Visual Studio Test Explorer
- `dotnet test` command
- NUnit Test Adapter

## Test Resources

Test resources (PDF files) should be placed in the `resources` directory. The `columns.pdf` file is used as a sample test document.

## Notes

- Tests that require OCR are disabled by default (`useOcr: false`) to avoid dependencies on OCR libraries
- Some tests may require specific PDF files in the resources directory
- Tests follow the Arrange-Act-Assert pattern
