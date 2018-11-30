using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace URLServerManagerModern.Utilities.IO
{
    public sealed class CSVReader : IDisposable
    {
        private enum ReadState {
            Initial,
            Reading,
            EOF
        }

        private ReadState readState = ReadState.Initial;

        private StreamReader streamReader;

        /**
         * <summary>
         * Sets whether the file has a header on the first line.
         * </summary>
         * <remarks>
         * This property has to be set true to consider first line as a header.
         * </remarks>
         **/
        public bool IncludesHeader { get; set; }

        private UTF8Encoding encoding = new UTF8Encoding();

        /**
         * <summary>
         * Initializes a new instance of CSVReader and creates a file stream with UTF-8 encoding for specified filename
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
        public CSVReader(string filePath)
        {
            streamReader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read), encoding);
            IncludesHeader = false;
        }

        /**
         * <summary>
         * Returns an array of entry properties.
         * </summary>
         * <exception cref="IOException"/>
         **/
        StringBuilder builder = new StringBuilder();
        List<string> entryOutput = new List<string>();
        
        public string[] ReadEntry()
        {
            entryOutput.Clear();

            bool escaped = false;
            char c;
            int i;
            while ((i = streamReader.Read()) > -1)
            {
                c = (char)i;
                if (c == '"')
                {
                    if (escaped)
                    {
                        if ((char)streamReader.Peek() != '"')
                        {
                            escaped = false;
                            continue;
                        }
                        else
                        {
                            escaped = true;
                            c = (char)streamReader.Read();
                        }
                    }
                    else
                    {
                        escaped = true;
                        continue;
                    }
                }

                if (escaped)
                    builder.Append(c);
                else
                {
                    if (c == ',')
                    {
                        entryOutput.Add(builder.ToString());
                        builder.Clear();
                    }
                    else if (c == '\r')
                    {
                        if ((char)streamReader.Peek() == '\n')
                        {
                            streamReader.Read();
                            if (readState == ReadState.Initial && IncludesHeader)
                                builder.Clear();
                            else
                            {
                                entryOutput.Add(builder.ToString());
                                break;
                            }
                        }
                        else
                            builder.Append(c);
                    }
                    else
                        builder.Append(c);
                }
            }

            if (readState == ReadState.Initial)
                readState++;
            else if (i == -1)
                readState = ReadState.EOF;

            builder.Clear();
            return entryOutput.ToArray();
        }

        public void Dispose()
        {
            streamReader.Close();

            builder.Clear();
            entryOutput.Clear();
            readState = ReadState.Initial;
        }
    }
}
