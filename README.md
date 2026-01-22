The goal of the TransactionObect design is to provide a simple interface for working with raw state ELT data.
It easy to address specific data points within the transaction by name aiding in both manilupation and addressing questions
to the relevant DMV in their own terms.

The TransactionObject is designed to parse and build both fixed format and delimited state ELT formats.

When parsing a raw transaction the data is returned as a OrderedDictionary where the 
keys are the fieldname and the associated values are the value extracted from the raw data.

The format and positions of each data point are determined at time of instantiation.  

To create a new TransactionObject for a different state, it simply a matter of updating the namespaces, the 
initialization function name, and properties state, format, and delimiter as appropriate.
     
Functions:
     ParseTransaction: Accepts RawTrans and uses Fields to parse the transaction into OrderedDictionary ParsedFields.
     BuildNew: Accepts an OrderedDictionary and builds a new transaction allowing for easy editing of existing transactions.
     toTABDelimited: Accepts an OrderedDictionary and returns a single TAB delimited string, this is very useful for converting raw 
         state files into a format easily imported to an SQL table for detail analysis en mass.
     toTable: Accepts an OrderedDictionary and returns a string containing the data as an HTML table for display.
     toForm: Accepts an OrderedDictionary and returns a string containing the data as an HTML for table suitable for 
         easy editing of any field.
     RedactPII: Redact fields identified in array PIIFields 
     redactField: Replace all chars in string after position 4 with "X"
   
