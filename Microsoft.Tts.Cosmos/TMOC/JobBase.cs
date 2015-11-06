//----------------------------------------------------------------------------
// <copyright file="JobBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines the JobBase class which is the base of cosmos job.
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Cosmos.TMOC
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Text;
    using System.Text.RegularExpressions;
    using ScopeRuntime;

    /// <summary>
    /// Job base.
    /// </summary>
    public class JobBase
    {
        #region
        private const int BufferSize = 16 * 1024; // 16KB.
        private Dictionary<string, string> _replacedVariable = null;
        private List<string> _replacedLoopVariable = null;
        private string _resourceScript;
        #endregion

        /// <summary>
        /// Gets or sets replace variable.
        /// </summary>
        public Dictionary<string, string> ReplaceVariable
        {
            get
            {
                return _replacedVariable;
            }

            set
            {
                if (value != null)
                {
                    _replacedVariable = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets loop variable.
        /// </summary>
        public List<string> ReplacedLoopVariable
        {
            get
            {
                return _replacedLoopVariable;
            }

            set
            {
                if (value != null)
                {
                    _replacedLoopVariable = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the resource script.
        /// </summary>
        public string ResourceScript
        {
            get
            {
                return _resourceScript;
            }

            set
            {
                if (value != null)
                {
                    _resourceScript = value;
                }
            }
        }

        /// <summary>
        /// Generate local file.
        /// </summary>
        /// <param name="fName">The name of file.</param>
        /// <param name="binary">The binary array to be written in file.</param>
        /// <param name="fileExtension">The extension of file.</param>
        /// <param name="filePath">The path of file.</param>
        /// <returns>The file path of generated file.</returns>
        public static string GenerateLocalFile(string fName, byte[] binary, string fileExtension, string filePath = "./")
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            string fname = Path.Combine(filePath, fName + "." + fileExtension);
            TmocDirectory.CreateForFile(fname);
            File.WriteAllBytes(fname, binary);
            return fname;
        }

        /// <summary>
        /// Generate Local File.
        /// </summary>
        /// <param name="fName">The name of file.</param>
        /// <param name="text">The text to be written in file.</param>
        /// <param name="fileExtension">The extension name of file.</param>
        /// <param name="fNeedReplaceSpace">The flag indicating whether need to replace space.</param>
        /// <param name="filePath">The path file.</param>
        /// <returns>The file generated path.</returns>
        public static string GenerateLocalFile(string fName, string text, string fileExtension, bool fNeedReplaceSpace = true, string filePath = "./")
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            string fname = Path.Combine(filePath, fName + "." + fileExtension);
            TmocDirectory.CreateForFile(fname);
            if (fNeedReplaceSpace)
            {
                text = Regex.Replace(text, @"\s", "\n");
            }

            File.WriteAllText(fname, text);
            return fname;
        }

        /// <summary>
        /// Generate text file.
        /// </summary>
        /// <param name="fName">The name of file.</param>
        /// <returns>The content of file.</returns>
        public static string GetTextFile(string fName)
        {
            double[] texts = File.ReadAllLines(fName).Select(x => double.Parse(x)).ToArray();
            StringBuilder sb = new StringBuilder();
            foreach (var text in texts)
            {
                sb.Append(text.ToString());
                sb.Append(" ");
            }

            return sb.ToString();
        }

        /// <summary>
        /// String to byte.
        /// </summary>
        /// <param name="hexString">The hex string.</param>
        /// <returns>The binary content of file.</returns>
        public static byte[] StringToByte(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("The binary key cannot have an odd number of digits: {0}", hexString);
            }

            List<byte> hexAsBytes = new List<byte>();
            for (int index = 0; index < hexString.Length / 2; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                if (string.Compare("\r\n", byteValue) != 0)
                {
                    hexAsBytes.Add(byte.Parse(byteValue, System.Globalization.NumberStyles.HexNumber));
                }
            }

            return hexAsBytes.ToArray();
        }

        /// <summary>
        /// TextFileToBinaryFile.
        /// </summary>
        /// <param name="textFilePath">The file path for string to byte convertion.</param>
        /// <param name="binaryFilePath">The file result output zip format convertion.</param>
        public static void TextFileToBinaryFile(string textFilePath, string binaryFilePath)
        {
            if (!File.Exists(textFilePath))
            {
                throw new FileNotFoundException("File not found", textFilePath);
            }

            using (FileStream outPutFs = new FileStream(binaryFilePath, FileMode.Create))
            {
                using (StreamReader reader = new StreamReader(textFilePath))
                {
                    char[] buffer = new char[BufferSize];
                    while (!reader.EndOfStream)
                    {
                        int bytesRead = reader.Read(buffer, 0, BufferSize);
                        if (bytesRead % 2 != 0)
                        {
                            throw new ArgumentException("The binary key cannot have an odd number of digits: {0}", textFilePath);
                        }

                        int byteArryIndex = 0;
                        int byteArrySize = bytesRead / 2;
                        byte[] hexBuffer = new byte[byteArrySize];
                        for (int strIndex = 0; strIndex < bytesRead / 2; strIndex++)
                        {
                            char firstChar = buffer[strIndex * 2];
                            char secondChar = buffer[(strIndex * 2) + 1];
                            string byteValue = firstChar.ToString() + secondChar.ToString();
                            if (string.Compare("\r\n", byteValue) != 0)
                            {
                                hexBuffer[byteArryIndex++] = byte.Parse(byteValue, System.Globalization.NumberStyles.HexNumber);
                            }
                        }

                        outPutFs.Write(hexBuffer.ToArray(), 0, byteArryIndex);
                    }
                }
            }
        }

        /// <summary>
        /// BinaryFileToTextFile.
        /// </summary>
        /// <param name="binaryFilePath">The file path for byte to string convertion.</param>
        /// <param name="textFilePath">The file result string format convertion.</param>
        public static void BinaryFileToTextFile(string binaryFilePath, string textFilePath)
        {
            if (!File.Exists(binaryFilePath))
            {
                throw new FileNotFoundException("File not found", textFilePath);
            }

            if (File.Exists(textFilePath))
            {
                File.Delete(textFilePath);
            }

            using (StreamWriter outPutFs = new StreamWriter(textFilePath, true))
            {
                using (FileStream reader = File.OpenRead(binaryFilePath))
                {
                    BinaryReader modelBr = new BinaryReader(reader);
                    byte[] byteCache = modelBr.ReadBytes(BufferSize);

                    while (byteCache != null && byteCache.Length > 0)
                    {
                        string hex = BitConverter.ToString(byteCache);
                        outPutFs.Write(hex.Replace("-", string.Empty));
                        byteCache = modelBr.ReadBytes(BufferSize);
                    }
                }
            }
        }

        /// <summary>
        /// Convert bytes to string.
        /// </summary>
        /// <param name="bytesArray">The array of bytes.</param>
        /// <returns>The string.</returns>
        public static string ByteToString(byte[] bytesArray)
        {
            string hex = BitConverter.ToString(bytesArray);
            return hex.Replace("-", string.Empty);
        }

        /// <summary>
        /// Read the binary of file.
        /// </summary>
        /// <param name="fName">The name of file.</param>
        /// <returns>The array of binary.</returns>
        public static byte[] GetBinaryFile(string fName)
        {
            return File.ReadAllBytes(fName);
        }

        /// <summary>
        /// Generate template.
        /// </summary>
        /// <returns>The returned template.</returns>
        public string GenerateTemplate()
        {
            string template = string.Empty;
            if (!string.IsNullOrEmpty(_resourceScript))
            {
                Stream stream = null;
                try
                {
                    stream = Assembly.GetCallingAssembly().GetManifestResourceStream(_resourceScript);

                    using (StreamReader txtreader = new StreamReader(stream))
                    {
                        stream = null;
                        template = txtreader.ReadToEnd();
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }

            if (_replacedVariable != null)
            {
                StringBuilder varreplacelist = new StringBuilder();
                varreplacelist.AppendLine("Setting script variables:");

                // Replace the variable which is enclosed with @@.
                foreach (var de in _replacedVariable)
                {
                    int hitCount = 0;
                    string varname = "@@" + de.Key.ToString() + "@@";
                    string val = (de.Value ?? string.Empty).ToString();
                    template = Regex.Replace(template, varname, m =>
                    {
                        hitCount++;
                        return val;
                    });

                    varreplacelist.AppendLine(string.Format("  {0} [{1}x]: {2}", de.Key.ToString(), hitCount, val));
                }

                // Split the resource file which use @ symbol
                // This is to make sure the resource file is not over the upperbound size.
                var linesOfTempalte = template.Split(new char[] { '\n' });
                string templateEmpty = string.Empty;
                foreach (var line in linesOfTempalte)
                {
                    if (line.Contains("RESOURCE") && line.Contains(":"))
                    {
                        var subPart = line.Split(new char[] { ':', ' ' });
                        var key = subPart[2].Trim(new char[] { '\r', '\n', '@', ';' }).ToUpper();
                        string count = _replacedVariable[key];
                        key = subPart[1].Trim(new char[] { '@' }).ToUpper();
                        string file = _replacedVariable[key];
                        for (int i = 0; i < int.Parse(count); i++)
                        {
                            templateEmpty += "RESOURCE @\"" + file + ".zip" + i.ToString() + "\";\n";
                        }
                    }
                    else if (line.Contains("RESOURCE") && line.Contains("?"))
                    {
                        var subPart = line.Split(new char[] { ' ' });
                        var key = subPart[1].Trim(new char[] { '\r', '\n', '@', ';', '?' }).ToUpper();
                        string value = _replacedVariable[key];
                        if (string.IsNullOrEmpty(value))
                        {
                            continue;
                        }

                        templateEmpty += line.Replace("?", string.Empty) + "\n";
                    }
                    else if (line.Contains("$") && _replacedLoopVariable != null)
                    {
                        // This part is to replace loop variable in the template
                        // For example, if the template has the variable like $name
                        // , it's replaced one by one by the string in the 
                        // _replacedLoopVariable list. 
                        // Find the variable
                        int start = line.IndexOf('$');
                        int end = line.IndexOfAny(new char[] { ' ', '\n', ';' }, start);
                        var subPart = line.Substring(start, end - start);
                        foreach (var loopedVar in _replacedLoopVariable)
                        {
                            var replacedLine = line.Replace(subPart, loopedVar);
                            templateEmpty += replacedLine + "\n";
                        }
                    }
                    else
                    {
                        templateEmpty += line + "\n";
                    }
                }

                if (!string.IsNullOrEmpty(templateEmpty))
                {
                    template = templateEmpty;
                }
            }
            else
            {
                throw new ArgumentNullException(string.Format("The variable  should not be null"));
            }

            return template;
        }
    }

    /// <summary>
    /// Job Processor.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class JobProcessor : Processor
    {
        private JobBase _job = new JobBase();

        /// <summary>
        /// Gets the instance of the job.
        /// </summary>
        public JobBase Job
        {
            get
            {
                return _job;
            }
        }

        /// <summary>
        /// Produces the output schema.
        /// </summary>
        /// <param name="requestedColumns">The requested columns.</param>
        /// <param name="args">The argument.</param>
        /// <param name="inputSchema">The input schema.</param>
        /// <returns>The output schema.</returns>
        public override Schema Produces(string[] requestedColumns, string[] args, Schema inputSchema)
        {
            Schema outputSchema = new Schema();
            return outputSchema;
        }

        /// <summary>
        /// Processing function.
        /// </summary>
        /// <param name="input">The input row.</param>
        /// <param name="outputRow">The output row.</param>
        /// <param name="args">The argument.</param>
        /// <returns>The output IEnumerable row.</returns>
        public override IEnumerable<Row> Process(RowSet input, Row outputRow, string[] args)
        {
            foreach (var row in input.Rows)
            {
                yield return outputRow;
            }
        }
    }

    /// <summary>
    /// Job Reducer.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class JobReducer : Reducer
    {
        private JobBase _job = new JobBase();

        /// <summary>
        /// Gets the instance of the job.
        /// </summary>
        public JobBase Job
        {
            get
            {
                return _job;
            }
        }

        /// <summary>
        /// Reduce.
        /// </summary>
        /// <param name="input">The input row.</param>
        /// <param name="outputRow">The output row.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The IEnumerable row.</returns>
        public override IEnumerable<Row> Reduce(RowSet input, Row outputRow, string[] args)
        {
            foreach (var row in input.Rows)
            {
                yield return outputRow;
            }
        }

        /// <summary>
        /// Schema of output.
        /// </summary>
        /// <param name="requestedColumns">The request columns.</param>
        /// <param name="args">The argument.</param>
        /// <param name="inputSchema">The input schema.</param>
        /// <returns>The output schema.</returns>
        public override Schema Produces(string[] requestedColumns, string[] args, Schema inputSchema)
        {
            Schema outputSchema = new Schema("AccIndex:int, HmmContent:string");
            return outputSchema;
        }
    }
}
