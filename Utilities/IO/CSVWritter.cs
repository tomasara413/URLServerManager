using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace URLServerManagerModern.Utilities.IO
{
    public sealed class CSVWritter : IDisposable
    {
        private StreamWriter streamWritter;

        private UTF8Encoding encoding = new UTF8Encoding();

        public bool IncludesHeader { get; }

        /**
         * <summary>
         * Initializes a new instance of CSVWritter and creates a file stream with UTF-8 encoding for specified filename
         * </summary>
         * <exception cref="ArgumentNullException"/>
         * <exception cref="ArgumentException"/>
         * <exception cref="NotSupportedException"/>
         * <exception cref="FileNotFoundException"/>
         * <exception cref="IOException"/>
         * <exception cref="System.Security.SecurityException"/>
         * <exception cref="DirectoryNotFoundException"/>
         * <exception cref="UnauthorizedAccessException"/>
         * <exception cref="PathTooLongException"/>
         * <exception cref="ArgumentOutOfRangeException"/>
         **/
        public CSVWritter(string filePath)
        {
            streamWritter = new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write), encoding);
            IncludesHeader = false;
            MaximumFields = -1;
        }

        public int MaximumFields { get; set; }
        /**
         * <summary>
         * Initializes a new instance of CSVWritter, creates a file stream with UTF-8 encoding for specified filename and writes specified header into the file
         * </summary>
         * <exception cref="ArgumentNullException"/>
         * <exception cref="ArgumentException"/>
         * <exception cref="NotSupportedException"/>
         * <exception cref="FileNotFoundException"/>
         * <exception cref="IOException"/>
         * <exception cref="System.Security.SecurityException"/>
         * <exception cref="DirectoryNotFoundException"/>
         * <exception cref="UnauthorizedAccessException"/>
         * <exception cref="PathTooLongException"/>
         * <exception cref="ArgumentOutOfRangeException"/>
         **/
        public CSVWritter(string filePath, params object[] header) : this(filePath)
        {
            WriteEntry(header);
            MaximumFields = header.Length;
            IncludesHeader = true;
        }

        /**
         * <summary>
         * Writes an entry of input parameters. If the parameter is of type string, it will be encased in double quotes
         * </summary>
         * <exception cref="IOException"/>
         **/
        StringBuilder builder = new StringBuilder();
        public void WriteEntry(params object[] parameters)
        {
            if (!IncludesHeader && MaximumFields < 0)
                MaximumFields = parameters.Length;

            if (parameters.Length != MaximumFields)
                throw new CSVException("Writting this record would cause the csv file to be invalid. All entries must have the same ammount of fields");

            string s;
            object o;
            for (int i = 0; i < MaximumFields; i++)
            {
                o = parameters[i];
                if (o != null)
                {
                    if ((s = o as string) != null)
                        builder.Append("\"").Append(s.Replace("\"", "\"\"")).Append("\"");
                    else
                        builder.Append(o);
                }
                builder.Append(",");
            }

            if(builder.Length > 0)
                builder.Length--;
            streamWritter.Write(builder.Append("\r\n").ToString());

            builder.Clear();
        }

        public void Dispose()
        {
            streamWritter.Close();

            builder.Clear();
        }
    }

    public class CSVException : Exception
    {
        public CSVException(string exception) : base(exception) { }
    }
}
