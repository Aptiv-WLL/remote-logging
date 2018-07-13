using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Security;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace Global.Logging
{
    public class Log
    {
        private static string fileName;                     // Log file name
        private static string directory;                    // Directory log file is saved to
        private static string fullPath;                     // Full path to log file, should be the absolute path
        private static bool timeStampInclude;               // Timeset default set to true
        private static NamedPipeClientStream connection;    // Pipe client to connect with server

        /// <summary>
        /// Constructor only run once before first access of class
        /// </summary>
        static Log()
        {
            timeStampInclude = true;
            AppDomain currentAppDomain = AppDomain.CurrentDomain; // Current application calling this module
            currentAppDomain.FirstChanceException += new EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>(LogExceptions); // Adding handler to catch all exceptions thrown
            GenerateLogFile();
            fullPath = Path.GetFullPath(fullPath);
        }

        ~Log()
        {

        }

        /// <summary>
        /// Generate initial log file
        /// </summary>
        private static void GenerateLogFile()
        {
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            fileName = "Log_" + timeStamp + ".txt";
            directory = Directory.GetCurrentDirectory();
            string path = Path.Combine(directory, fileName);

            // Creates a log file with basic information about the application that uses Logging.dll
            if (!File.Exists(path))
            {
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    Assembly assem = Assembly.GetEntryAssembly();
                    if(assem == null)
                    {
                        //For when the starting program isn't managed (eg like unittests)
                        assem = new StackTrace().GetFrames().Last().GetMethod().Module.Assembly;
                    }
                    sw.WriteLine("Log File - " + assem.FullName);
                    sw.WriteLine("File Created: " + timeStamp + "\n");
                    sw.Close();
                }
                fullPath = path;
            }
            else
            {
                Console.WriteLine("Error: File already exists.");
            }

        }

        /// <summary>
        /// Re-Save log file with current name
        /// (Can be used to re-save log file in new directory
        /// w/o changing file name)
        /// </summary>
        public static void Save()
        {
            SaveAs(fileName);
        }     

        /// <summary>
        /// Save Log File under New Name
        /// Calls method in Logging Display to update file path.
        /// Instantiates NamedPipeClientStream and tries to connect to 
        /// a server if there is one available.
        /// </summary>
        /// <param name="newFileName">New Name of File</param>
        public static void SaveAs(string newFile)
        {
            if (connection == null)
            {
                connection = new NamedPipeClientStream(".", fileName, PipeDirection.Out, PipeOptions.WriteThrough);
            }

            if (connection != null && !connection.IsConnected)
            {
                try
                {
                    connection.Connect(500);
                }
                catch (TimeoutException)
                {
                    //WriteLine(e.ToString());
                    connection = new NamedPipeClientStream(".", newFile, PipeDirection.Out, PipeOptions.WriteThrough);
                }
            }

            string newFileName = newFile;
            // checks if file name already has extension, if not, add on a .txt
            if (!Path.HasExtension(newFileName))
            {
                newFileName += ".txt";
            }
            string path = Path.Combine(directory, newFileName);
            // if file does not already exist in new directory, copy
            // file and then delete old file
            if (!File.Exists(path))
            {
                if (connection != null && connection.IsConnected)
                {
                    try
                    {
                        StreamWriter sw = new StreamWriter(connection);
                        sw.WriteLine(path);
                        sw.Flush();
                    }
                    catch (IOException)
                    {
                        try
                        {
                            //WriteLine(e.ToString());
                            connection.Close();

                            connection = new NamedPipeClientStream(".", fileName, PipeDirection.Out, PipeOptions.WriteThrough);
                            connection.Connect(500);

                        }
                        catch (TimeoutException)
                        {
                            //WriteLine(e1.ToString());
                            connection = new NamedPipeClientStream(".", newFile, PipeDirection.Out, PipeOptions.WriteThrough);
                        }
                    }
                }
                File.Copy(fullPath, path);
                File.Delete(fullPath);
                fullPath = path;
                fileName = newFile;
            }
            else
            {
                Console.WriteLine("Error: {0} already exists.", newFileName);
            }
        }

        /// <summary>
        /// Toggle whether or not time stamp is included
        /// </summary>
        /// <param name="value">Time stamp included or not</param>
        public static void ToggleTimeStamp(bool value)
        {
            timeStampInclude = value;
        }

        /// <summary>
        /// Creates new directory if it does not already exist
        /// Sets new directory to save log file in (Does not actually save file)
        /// </summary>
        /// <param name="folderPath">New Directory Path</param>
        public static void SetLogFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Console.WriteLine("Folder {0} is a valid directory. Setting new file path.", folderPath);
                directory = folderPath;
                Save();
            }
            else
            {
                Console.WriteLine("Folder {0} is not an existing directory. Setting new file path.", folderPath);
                Directory.CreateDirectory(folderPath);
                directory = folderPath;
            }
            fullPath = Path.GetFullPath(fullPath);
        }

        #region --- Write ---

        /// <summary>
        /// Writes specified Unicode character value to log file.
        /// </summary>
        /// <param name="value">Unicode character</param>
        public static void Write(char value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes specified array of Unicode characters to log file.
        /// </summary>
        /// <param name="buffer">Unicode character array</param>
        public static void Write(char[] buffer)
        {
            Write(new string(buffer));
        }

        /// <summary>
        /// Writes text representation of the specified System.Decimal value to log file.
        /// </summary>
        /// <param name="value">Decimal value</param>
        public static void Write(decimal value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified 32-bit signed integer value to
        /// the log file.
        /// </summary>
        /// <param name="value">32-bit signed integer</param>
        public static void Write(int value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified 64-bit signed integer value to the
        /// log file.
        /// </summary>
        /// <param name="value">64-bit signed integer</param>
        public static void Write(long value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes the text represenation of the specified object to the log file.
        /// </summary>
        /// <param name="value">The object or null</param>
        public static void Write(object value)
        {
            if (value == null)
            {
                Write("null");
            }
            else
            {
                Write(value.ToString());
            }
        }
        
        /// <summary>
        /// Writes the text represenation of the specified 64-bit unsigned integer value
        /// to the log file
        /// </summary>
        /// <param name="value">64-bit unsigned integer</param>
        public static void Write(ulong value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified 32-bit unsigned integer value
        /// to the log file.
        /// </summary>
        /// <param name="value">32-bit unsiged integer</param>
        public static void Write(uint value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified single-precision floating-point
        /// value to the log file.
        /// </summary>
        /// <param name="value">Single-precision floating-point</param>
        public static void Write(float value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified double-precision floating-point value
        /// to the log file.
        /// </summary>
        /// <param name="value">Double-precision floating-point</param>
        public static void Write(double value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified Boolean value to the log file.
        /// </summary>
        /// <param name="value">Boolean</param>
        public static void Write(bool value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes the string to the log file.
        /// </summary>
        /// <param name="value">String</param>
        public static void Write(string value)
        {
            Write(data: value);
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects to
        /// the log file using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg">Array of objects to write using format</param>
        public static void Write(string format, params object[] arg)
        {
            Write(string.Format(format, arg));
        }

        /// <summary>
        /// Writes the text representation of the specified object to the log file
        /// using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg0">Object to write using format</param>
        public static void Write(string format, object arg0)
        {
            Write(string.Format(format, arg0));
        }

        /// <summary>
        /// Writes the specified subarray of Unicode characters to the log file.
        /// </summary>
        /// <param name="buffer">Array of Unicode characters</param>
        /// <param name="index">Starting position in buffer</param>
        /// <param name="count">Number of characters to write</param>
        public static void Write(char[] buffer, int index, int count)
        {
            Write(new string(buffer, index, count));

        }

        /// <summary>
        /// Writes the text representation of the specified objects to the log file
        /// using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg0">First object to write using format</param>
        /// <param name="arg1">Second object to write using format</param>
        public static void Write(string format, object arg0, object arg1)
        {
            Write(string.Format(format, arg0, arg1));
        }

        /// <summary>
        /// Writes the text representation of the specified objects to the log file
        /// using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg0">First object to write using format</param>
        /// <param name="arg1">Second object to write using format</param>
        /// <param name="arg2">Second object to write using format</param>
        public static void Write(string format, object arg0, object arg1, object arg2)
        {
            Write(string.Format(format, arg0, arg1, arg2));
        }

        /// <summary>
        /// Writes the text representation of the specified objects to the log file
        /// using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg0">First object to write using format</param>
        /// <param name="arg1">Second object to write using format</param>
        /// <param name="arg2">Second object to write using format</param>
        /// <param name="arg3">Third object to write using format</param>
        [SecuritySafeCritical]
        public static void Write(string format, object arg0, object arg1, object arg2, object arg3)
        {
            Write(string.Format(format, arg0, arg1, arg2, arg3));
        }
        
        /// <summary>
        /// Writes data into log file without a new line
        /// </summary>
        /// <param name="data">Data to be Written</param>
        private static void Write(params string[] data)
        {
            using (FileStream fw = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                string time = null;
                byte[] info = null;

                if (timeStampInclude)
                {
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss  ");
                    info = new UTF8Encoding(true).GetBytes(time);
                }
                
                for (int i = 0; i < data.Length; i++)
                {
                    if (timeStampInclude)
                    {
                        fw.Write(info, 0, info.Length);
                    }
                    byte[] dataBytes = new UTF8Encoding(true).GetBytes(data[i]);
                    fw.Write(dataBytes, 0, dataBytes.Length);
                }
            }
        }

        #endregion

        #region --- WriteLine ---

        /// <summary>
        /// Writes the text representation of the specified 64-bit signed integer value,
        /// followed by the current line terminator, to the log file.
        /// </summary>
        /// <param name="value">64-bit signed integer</param>
        public static void WriteLine(long value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the text represenation of the specified 64-bit unsigned integer value,
        /// followed by the current line terminator, to the log file
        /// </summary>
        /// <param name="value">64-bit unsigned integer</param>
        public static void WriteLine(ulong value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the text represenation of the specified object,
        /// followed by the current line terminator, to the log file.
        /// </summary>
        /// <param name="value">The object or null</param>
        public static void WriteLine(object value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes text representation of the specified System.Decimal value,
        /// followed by the current line terminator, to log file.
        /// </summary>
        /// <param name="value">Decimal value</param>
        public static void WriteLine(decimal value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified double-precision floating-point value,
        /// followed by the current line terminator, to the log file.
        /// </summary>
        /// <param name="value">Double-precision floating-point</param>
        public static void WriteLine(double value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified single-precision floating-point,
        /// followed by the current line terminator, value to the log file.
        /// </summary>
        /// <param name="value">Single-precision floating-point</param>
        public static void WriteLine(float value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified 32-bit unsigned integer value,
        /// followed by the current line terminator, to the log file.
        /// </summary>
        /// <param name="value">32-bit unsiged integer</param>
        public static void WriteLine(uint value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified 32-bit signed integer value,
        /// followed by the current line terminator, to the log file.
        /// </summary>
        /// <param name="value">32-bit signed integer</param>
        public static void WriteLine(int value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes specified array of Unicode characters,
        /// followed by the current line terminator, to log file.
        /// </summary>
        /// <param name="buffer">Unicode character array</param>
        public static void WriteLine(char[] buffer)
        {
            WriteLine(new string(buffer));
        }

        /// <summary>
        /// Writes specified Unicode character value,
        /// followed by the current line terminator, to log file.
        /// </summary>
        /// <param name="value">Unicode character</param>
        public static void WriteLine(char value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the text representation of the specified Boolean value,
        /// followed by the current line terminator, to the log file.
        /// </summary>
        /// <param name="value">Boolean</param>
        public static void WriteLine(bool value)
        {
            WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the string, followed by the current line terminator, to the log file.
        /// </summary>
        /// <param name="value">String</param>
        public static void WriteLine(string value)
        {
            WriteLine(data: value);
        }

        /// <summary>
        /// Writes the text representaiton of the specified array of objects,
        /// followed by the current line terminator, to
        /// the log file using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg">Array of objects to write using format</param>
        public static void WriteLine(string format, params object[] arg)
        {
            WriteLine(string.Format(format, arg));
        }
        
        /// <summary>
        /// Writes the text representation of the specified object,
        /// followed by the current line terminator, to the log file
        /// using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg0">Object to write using format</param>
        public static void WriteLine(string format, object arg0)
        {
            WriteLine(string.Format(format, arg0));
        }

        /// <summary>
        /// Writes the text representation of the specified objects,
        /// followed by the current line terminator, to the log file
        /// using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg0">First object to write using format</param>
        /// <param name="arg1">Second object to write using format</param>
        public static void WriteLine(string format, object arg0, object arg1)
        {
            WriteLine(string.Format(format, arg0, arg1));
        }

        /// <summary>
        /// Writes the specified subarray of Unicode characters,
        /// followed by the current line terminator, to the log file.
        /// </summary>
        /// <param name="buffer">Array of Unicode characters</param>
        /// <param name="index">Starting position in buffer</param>
        /// <param name="count">Number of characters to write</param>
        public static void WriteLine(char[] buffer, int index, int count)
        {
            WriteLine(new string(buffer, index, count));
        }

        /// <summary>
        /// Writes the text representation of the specified objects,
        /// followed by the current line terminator, to the log file
        /// using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg0">First object to write using format</param>
        /// <param name="arg1">Second object to write using format</param>
        /// <param name="arg2">Second object to write using format</param>
        public static void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            WriteLine(string.Format(format, arg0, arg1, arg2));
        }

        /// <summary>
        /// Writes the text representation of the specified objects,
        /// followed by the current line terminator, to the log file
        /// using the specified format information.
        /// </summary>
        /// <param name="format">Composite format string</param>
        /// <param name="arg0">First object to write using format</param>
        /// <param name="arg1">Second object to write using format</param>
        /// <param name="arg2">Second object to write using format</param>
        /// <param name="arg3">Third object to write using format</param>
        [SecuritySafeCritical]
        public static void WriteLine(string format, object arg0, object arg1, object arg2, object arg3)
        {
            WriteLine(string.Format(format, arg0, arg1, arg2, arg3));
        }

        /// <summary>
        /// Writes data into new line in log file (Toggled Timestamp)
        /// </summary>
        /// <param name="data">Data to be Written</param>
        private static void WriteLine(params string[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] += System.Environment.NewLine;
            }
            Write(data);
        }

        #endregion

        /// <summary>
        /// Returns the full path to log file as a string
        /// </summary>
        /// <returns>Full path to log file</returns>
        public static string GetFilePath()
        {
            return fullPath;
        }

        /// <summary>
        /// Returns the log file name as a string
        /// </summary>
        /// <returns>Log file name</returns>
        public static string GetFileName()
        {
            return fileName;
        }

        /// <summary>
        /// Returns the path to directory as a string
        /// </summary>
        /// <returns>Path to directory</returns>
        public static string GetDirectory()
        {
            return directory;
        }
        
        /// <summary>
        /// Logs any thrown exception in the main application running
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void LogExceptions(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs args)
        {
            WriteLine(args.Exception.ToString());
            //WriteLine(args.Exception.StackTrace);
        }
    }
}