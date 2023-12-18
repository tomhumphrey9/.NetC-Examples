using System.Reflection;

/*
    Conversion method betweet MYOB Advanced data and DocuWare index fields
*/


namespace MyobAdvancedFunction
{
    class MYOBDataToFields
    {
        public static List<Fields> dataToFields<T>(T myobData)
        {
            if (myobData == null) { throw new Exception("myobData is NULL"); }

            List<Fields> LF = new List<Fields>();

            PropertyInfo[] infos = myobData.GetType().GetProperties();

            foreach (PropertyInfo info in infos)
            {

                Fields F = new Fields();
                F.FieldName = info.Name.ToUpper();

                if (F.FieldName == "ID")
                {
                    var idValue = info.GetValue(myobData);
                    F.Item = idValue != null ? idValue.ToString() : "";
                    F.ItemElementName = "String";
                    LF.Add(F);
                }
                else if (F.FieldName != "ROWNUMBER") // Skip rowNumber
                {
                    // Extract value property from complex types
                    var complexProperty = info.GetValue(myobData);
                    var valueProperty = complexProperty?.GetType().GetProperty("value");
                    var value = valueProperty?.GetValue(complexProperty);

                    if (value is string stringValue)
                    {
                        F.Item = stringValue;
                        F.ItemElementName = "String";
                    }
                    else if (value is bool boolValue)
                    {
                        F.Item = boolValue.ToString().ToLower(); // "true" or "false" as a string
                        F.ItemElementName = "String"; 
                    }
                    else if (value is int intValue)
                    {
                        F.Item = intValue;
                        F.ItemElementName = "Int";
                    }
                    else if (value is DateTime dateTimeValue)
                    {
                        // Include date, time, fractional seconds, and time zone information
                        F.Item = dateTimeValue.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK");
                        F.ItemElementName = "DateTime"; 
                    }
                    else if (value is double doubleValue)
                    {
                        F.Item = Math.Round(doubleValue, 2);
                        F.ItemElementName = "Decimal";
                    }
                    LF.Add(F);
                }
            }
            return LF;
        }
    }
}
