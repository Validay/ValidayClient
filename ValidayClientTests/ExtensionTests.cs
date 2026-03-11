using System.ComponentModel;
using Xunit;
using ValidayClient.Extensions;
using ValidayClient.Logging;

namespace ValidayClientTests
{
    public class ExtensionTests
    {
        enum NoDescriptionEnum { Value }

        [Description("Custom Label")]
        enum SingleDescribedEnum { [Description("Custom Label")] Value }

        [Fact]
        public void GetDisplayName_WithDescriptionAttribute_ReturnsDescription()
        {
            Assert.Equal("Low", LogType.Low.GetDisplayName());
            Assert.Equal("Info", LogType.Info.GetDisplayName());
            Assert.Equal("Warning", LogType.Warning.GetDisplayName());
            Assert.Equal("Error", LogType.Error.GetDisplayName());
            Assert.Equal("Critical Error", LogType.CriticalError.GetDisplayName());
        }

        [Fact]
        public void GetDisplayName_WithoutDescriptionAttribute_ReturnsEnumName()
        {
            Assert.Equal("Value", NoDescriptionEnum.Value.GetDisplayName());
        }

        [Fact]
        public void GetDisplayName_CustomDescription_ReturnsCustomLabel()
        {
            Assert.Equal("Custom Label", SingleDescribedEnum.Value.GetDisplayName());
        }
    }
}
