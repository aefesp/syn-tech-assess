using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Synapse;
using Xunit;

namespace PatientEquipmentProcessor.Tests
{
    public class ExtractionTests
    {
        /// <summary>
        /// Tests that ExtractDeviceType correctly identifies device types from prescription/recommendation text.
        /// This is a critical business logic test that ensures the core extraction functionality works correctly
        /// with various input formats and edge cases.
        /// </summary>
        [Theory]
        [InlineData("CPAP", "CPAP")]
        [InlineData("cpap", "CPAP")]
        [InlineData("CPAP therapy", "CPAP")]
        [InlineData("Patient needs a CPAP machine", "CPAP")]
        [InlineData("oxygen", "Oxygen Tank")]
        [InlineData("OXYGEN", "Oxygen Tank")]
        [InlineData("Requires a portable oxygen tank delivering 2 L per minute", "Oxygen Tank")]
        [InlineData("wheelchair", "Wheelchair")]
        [InlineData("WHEELCHAIR", "Wheelchair")]
        [InlineData("medication", "Unknown")]
        [InlineData("", "Unknown")]
        [InlineData(null, "Unknown")]
        public void ExtractDeviceType_ShouldReturnCorrectDeviceType(string? prescriptionText, string expected)
        {
            // Arrange
            var patientData = new Dictionary<string, string>();
            if (prescriptionText != null)
            {
                patientData["Prescription"] = prescriptionText;
            }

            // Act
            var result = Program.ExtractDeviceType(patientData);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Tests that BuildJsonPayload correctly constructs the JSON output with all fields and handles null values properly.
        /// This ensures the API receives correctly formatted data and null fields are excluded as expected.
        /// </summary>
        [Fact]
        public void BuildJsonPayload_WithCompleteOxygenTankData_ShouldIncludeAllFields()
        {
            // Arrange - Simulating a complete oxygen tank order matching expected_output1.json
            string deviceType = "Oxygen Tank";
            string orderingProvider = "Dr. Cuddy";
            string diagnosis = "COPD";
            string patientName = "Harold Finch";
            string dateOfBirth = "04/12/1952";
            string liters = "2 L";
            string usage = "sleep and exertion";

            // Act
            var result = Program.BuildJsonPayload(
                deviceType, orderingProvider, diagnosis, patientName, dateOfBirth, liters, usage);

            // Assert - Verify all fields are present and correct
            Assert.NotNull(result);
            Assert.Equal("Oxygen Tank", result["device"]?.ToString());
            Assert.Equal("Dr. Cuddy", result["ordering_provider"]?.ToString());
            Assert.Equal("COPD", result["diagnosis"]?.ToString());
            Assert.Equal("Harold Finch", result["patient_name"]?.ToString());
            Assert.Equal("04/12/1952", result["dob"]?.ToString());
            Assert.Equal("2 L", result["liters"]?.ToString());
            Assert.Equal("sleep and exertion", result["usage"]?.ToString());
        }

        [Fact]
        public void BuildJsonPayload_WithNullOptionalFields_ShouldExcludeNullFields()
        {
            // Arrange - Simulating a CPAP order with minimal data
            string deviceType = "CPAP";
            string orderingProvider = "Dr. Johnson";
            string diagnosis = "Sleep Apnea";
            string patientName = "John Doe";
            string dateOfBirth = "01/01/1980";
            string? liters = null;
            string? usage = null;

            // Act
            var result = Program.BuildJsonPayload(
                deviceType, orderingProvider, diagnosis, patientName, dateOfBirth, liters, usage);

            // Assert - Verify only required fields are present, null fields are excluded
            Assert.NotNull(result);
            Assert.Equal("CPAP", result["device"]?.ToString());
            Assert.Equal("Dr. Johnson", result["ordering_provider"]?.ToString());
            Assert.Equal("Sleep Apnea", result["diagnosis"]?.ToString());
            Assert.Equal("John Doe", result["patient_name"]?.ToString());
            Assert.Equal("01/01/1980", result["dob"]?.ToString());
            Assert.Null(result["liters"]);
            Assert.Null(result["usage"]);
        }
    }
}

