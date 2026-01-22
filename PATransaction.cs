using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PATransactionNameSpace
{
    /***********************************************************************************************************************************
     * 
     * The goal of the TransactionObect design is to provide a simple interface for working with raw state ELT data.
     * It easy to address specific data points within the transaction by name aiding in both manilupation and addressing questions
	 * to the relevant DMV in their own terms.
     * 
     * The TransactionObject is designed to parse and build both fixed format and delimited state ELT formats.
     * 
     * When parsing a raw transaction the data is returned as a OrderedDictionary where the 
     * keys are the fieldname and the associated values are the value extracted from the raw data.
     * 
     * The format and positions of each data point are determined at time of instantiation.  
     * 
     * To create a new TransactionObject for a different state, it simply a matter of updating the namespaces, the 
     * initialization function name, and properties state, format, and delimiter as appropriate.
     *
     * Functions:
     *      ParseTransaction: Accepts RawTrans and uses Fields to parse the transaction into OrderedDictionary ParsedFields.
     *      BuildNew: Accepts an OrderedDictionary and builds a new transaction allowing for easy editing of existing transactions.
     *      toTABDelimited: Accepts an OrderedDictionary and returns a single TAB delimited string, this is very useful for converting raw 
     *          state files into a format easily imported to an SQL table for detail analysis en mass.
     *      toTable: Accepts an OrderedDictionary and returns a string containing the data as an HTML table for display.
     *      toForm: Accepts an OrderedDictionary and returns a string containing the data as an HTML for table suitable for 
     *          easy editing of any field.
     *      RedactPII: Redact fields identified in array PIIFields 
     *      redactField: Replace all chars in string after position 4 with "X"
     *      
     ***************************************************************************************************/
    public class PATransaction
    {
        public bool debug = false;
        public string state = "PA";
        public string format = "FIXED";
        public string delimiter = "";
        public string rawTransaction = "";
        private string[] PIIFields = ["Owner_Name", "1st_Street_Address", "2nd_Street_Address"];

        // Fields is used record format in question
        public static OrderedDictionary Fields { get; set; } = new OrderedDictionary();

        // ParsedFields is used to store each distinct field and their values in the proper sequence.
        public static OrderedDictionary ParsedFields { get; set; } = new OrderedDictionary();

        // FieldNames contains the state format field names in order
        public static string[] FieldNames { get; set; } = new string[] {
            "Transaction_Type","ABA_Number","Title_Number","Title_Mod_11_Check_Digit","Owner_Check_Digits",
            "Owner_Name_Code","Owner_Name","1st_Street_Address","2nd_Street_Address","City","state","Zip_Code",
            "VIN_Number","Vehicle_Model_Year","Vehicle_Make_Model_Code","Vehicle_Model_Code","Vehicle_Body_Type_Code",
            "Unladen_Vehicle_Weight","Gross_Vehicle_Reg._Weight","Gross_Combined_Weight_Qty","Seat_Capacity_Quantity",
            "Odometer_Read_Quantity","Odometer_Qualify_Code","TB_Grey_Market_Indicator","TB_Antique_Classic_Code",
            "TB_Previous_Use_Code","TB_Reconstructed_Vehicle_Code","TB_Vehicle_Use_Code","TB_VIN_Reissue_Indicator",
            "TB_state_of_Origin","Lessee_Name_Code","Lessee_Name","WID_Number","Transaction_Error_Code",
            "Lien_Encumbrance_Date","Integrator_Code"
        };

        public PATransaction()
        {
            if (Fields.Count == 0)
            {
                Fields.Add("Transaction_Type", new int[2] { 0, 4 });
                Fields.Add("ABA_Number", new int[2] { 4, 11 });
                Fields.Add("Title_Number", new int[2] { 15, 8 });
                Fields.Add("Title_Mod_11_Check_Digit", new int[2] { 23, 1 });
                Fields.Add("Owner_Check_Digits", new int[2] { 24, 2 });
                Fields.Add("Owner_Name_Code", new int[2] { 26, 1 });
                Fields.Add("Owner_Name", new int[2] { 27, 58 });
                Fields.Add("1st_Street_Address", new int[2] { 85, 23 });
                Fields.Add("2nd_Street_Address", new int[2] { 108, 23 });
                Fields.Add("City", new int[2] { 131, 14 });
                Fields.Add("state", new int[2] { 145, 2 });
                Fields.Add("Zip_Code", new int[2] { 147, 5 });
                Fields.Add("VIN_Number", new int[2] { 152, 21 });
                Fields.Add("Vehicle_Model_Year", new int[2] { 173, 4 });
                Fields.Add("Vehicle_Make_Model_Code", new int[2] { 177, 4 });
                Fields.Add("Vehicle_Model_Code", new int[2] { 181, 3 });
                Fields.Add("Vehicle_Body_Type_Code", new int[2] { 184, 5 });
                Fields.Add("Unladen_Vehicle_Weight", new int[2] { 189, 5 });
                Fields.Add("Gross_Vehicle_Reg._Weight", new int[2] { 194, 5 });
                Fields.Add("Gross_Combined_Weight_Qty.", new int[2] { 199, 5 });
                Fields.Add("Seat_Capacity_Quantity", new int[2] { 204, 3 });
                Fields.Add("Odometer_Read_Quantity", new int[2] { 207, 7 });
                Fields.Add("Odometer_Qualify_Code", new int[2] { 214, 1 });
                Fields.Add("TB_Grey_Market_Indicator", new int[2] { 215, 1 });
                Fields.Add("TB_Antique_Classic_Code", new int[2] { 216, 1 });
                Fields.Add("TB_Previous_Use_Code", new int[2] { 217, 1 });
                Fields.Add("TB_Reconstructed_Vehicle_Code", new int[2] { 218, 1 });
                Fields.Add("TB_Vehicle_Use_Code", new int[2] { 219, 1 });
                Fields.Add("TB_VIN_Reissue_Indicator", new int[2] { 220, 1 });
                Fields.Add("TB_state_of_Origin", new int[2] { 221, 2 });
                Fields.Add("Lessee_Name_Code", new int[2] { 223, 1 });
                Fields.Add("Lessee_Name", new int[2] { 224, 58 });
                Fields.Add("WID_Number", new int[2] { 282, 18 });
                Fields.Add("Transaction_Error_Code", new int[2] { 300, 4 });
                Fields.Add("Lien_Encumbrance_Date", new int[2] { 304, 6 });
                Fields.Add("Integrator_Code", new int[2] { 310, 1 });
            }
        }

        public OrderedDictionary ParseTransaction(string RawTrans)
        {
            RawTrans.PadRight(311); // Pad to max size to avoid errors parsing malformed transactions
            if (debug) { Console.WriteLine("\n--- Parsing Fields ---"); }
            if (format == "FIXED")
            {
                foreach (var key in Fields.Keys)
                {
                    int[] Pos = (int[])Fields[key];
                    string fieldData = RawTrans.Substring(Pos[0], Pos[1]).TrimEnd(); ;
                    if (ParsedFields.Contains(key))
                    {
                        ParsedFields[key] = fieldData.TrimEnd();
                    }
                    else
                    {
                        ParsedFields.Add(key, fieldData.TrimEnd());
                    }
                    if (debug) { Console.WriteLine($"{key}: [{ParsedFields[key]}]"); }
                }
            }
            else
            {
                string[] Data = RawTrans.Split(delimiter);
                for (int pos = 0; pos < FieldNames.Length; pos++)
                {
                    ParsedFields[FieldNames[pos]] = Data[pos];
                }
            }
            return ParsedFields;
        }

        public string BuildNew(OrderedDictionary DataFields)
        { // Returns a string of the raw data values in the expected sequence
            string spaces = new string(' ', 60); // right pad fixed position fields to the expected length
            string newTransaction = "";
            if (format == "delimited")
            {
                foreach (object key in DataFields.Keys)
                {
                    string DataField = (string)DataFields[key] + spaces;
                    newTransaction += DataField + delimiter;
                }
                // Remove final delimiter
                newTransaction = newTransaction.Substring(0, newTransaction.Length - 1);
            }
            else
            //{
                foreach (object key in DataFields.Keys)
                {
                    int[] Pos = (int[])Fields[key];
                    string DataField = (string)DataFields[key] + spaces;
                    newTransaction += DataField.Substring(0, Pos[1]);
                    if (debug) { Console.WriteLine($">{newTransaction}"); }
                }
            //}
            return newTransaction;
        }

        public string toTABDelimited(OrderedDictionary DataFields)
        {   // Returns a string containing a TAB delimited version of the values
            // Could add argument delimiter to allow for multiple options
            string newTABDelimited = "";
            foreach (object key in DataFields.Keys)
            {
                string DataField = (string)DataFields[key];
                newTABDelimited += DataField.TrimEnd() + "\t";
            }
            return newTABDelimited;
        }

        public string toTable(OrderedDictionary DataFields)
        { // Returns and HTML table with 1 row per field including name, start, expected lenght, the lengh of the actual fied value and then the actual parsed feld value
            string newTable = "<table border = 1><th>Field</th><th>Start</th><th>Len</th><th>DataLen</th><th>Value</th></tr>\r\n";
            foreach (object key in DataFields.Keys)
            {
                int[] Pos = (int[])Fields[key];
                string DataField = (string)DataFields[key];
                newTable += $"<tr><td>{key}</td><td>{Pos[0]}</td><td>{Pos[1]}</td><td>{DataField.Length}</td><td>{DataField}</td></tr>\r\n";
            }
            newTable += "</table>";
            return newTable;
        }

        public string toForm(OrderedDictionary DataFields)
        {  // Returns and HTML table with 1 form input row per field including name, start, expected lenght, the lengh of the actual fied value and then the actual parsed feld value

            string newTable = "<table border = 1><th>Field</th><th>Start</th><th>Len</th><th>Size</th><th>Value</th></tr>\r\n";
            foreach (object key in DataFields.Keys)
            {
                int[] Pos = (int[])Fields[key];
                string DataField = (string)DataFields[key];
                newTable += $"<tr><td align='right'>{key}</td><td>{Pos[0]}</td><td>{Pos[1]}</td><td>{DataField.Length}</td><td><input type='text' id='{key}' name='{key}' maxlength='{Pos[1]}' size='{Pos[1]}' value='{DataField}' ></td></tr>\r\n";
            }
            newTable += "</table>";
            return newTable;
        }

        public OrderedDictionary RedactPII(OrderedDictionary DataFields)
        {	// Redact data in identified fields to remove PII and ensure compliance with DMV requirents / attestations
            foreach (string fieldName in PIIFields)
            {
                string fieldData = (string) DataFields[fieldName];
                DataFields[fieldName] = redactField(fieldData);
            }
            return DataFields;
        }

        private string redactField(string fieldData){
			// Replace all chars withing fieldData with "X" to remove PII
            char redactChar = 'X';
            if (fieldData.Length > 4) // 4 characters is generally not sufficient for usable PII
            {
                for (int ipos = 4; ipos < fieldData.Length; ipos++)
                {
                    StringBuilder sb = new StringBuilder(fieldData);
                    sb[ipos] = redactChar;
                    fieldData = sb.ToString();
                }
            }
            return fieldData;
        }
    }
}