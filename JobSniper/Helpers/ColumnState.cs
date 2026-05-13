using System.Text.Json.Serialization;
using System.Windows.Controls;

namespace JobSniper.Helpers
{
    public class ColumnState
    {
        public string Identifier { get; set; } = string.Empty;
        public double WidthValue { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataGridLengthUnitType WidthType { get; set; }

        public int DisplayIndex { get; set; }
    }
}