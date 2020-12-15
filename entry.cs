using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace JavaPackager {

    public class Program {

        public static readonly byte[] INDICATOR = Encoding.ASCII.GetBytes("//jbytecode//");

        public static readonly int OBF_SHIFT = 128;

        public static byte[] translate(byte[] bytes, int shift) {
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte) (bytes[i] + shift);
            return bytes;
        }

        [STAThread]
        public static void Main(string[] args) {
            bool gui = true, shell = false;
            string parsethrough = string.Empty;
            if(args.Length > 0) {
                foreach (string arg in args) {
                    if (arg.Equals("nogui")) gui = false;
                    else if (arg.Equals("shell")) shell = true;
                    parsethrough += ' ' + arg;
                }
            }
            string cwd = Environment.CurrentDirectory;
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string jar = Path.GetTempPath() + '\\' + exe.Substring(exe.LastIndexOf('\\') + 1).Replace(".exe", ".jar").Replace(".shell.jar", ".jar");
            bool build = false;
            byte[] bytes;
            using (FileStream stream = File.OpenRead(exe)) {
                bytes = new byte[stream.Length];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = (byte) stream.ReadByte();
                bytes = translate(bytes, -OBF_SHIFT);
                build = Encoding.ASCII.GetString(bytes).Contains(Encoding.ASCII.GetString(INDICATOR));
                stream.Close();
            }
            if (build) {
                int init = -1;
                byte[] code = new byte[0];
                for(int i = 0; i < bytes.Length; i++) {
                    byte[] check = new byte[INDICATOR.Length];
                    for (int ib = i; ib < i + check.Length && ib < bytes.Length; ib++) check[ib - i] = bytes[ib];
                    if (0 <= init) code[i - init] = bytes[i];
                    else if (Encoding.ASCII.GetString(check).Equals(Encoding.ASCII.GetString(INDICATOR))) {
                        i += INDICATOR.Length - 1;
                        init = i + 1;
                        code = new byte[bytes.Length - init];
                    }
                }
                using (FileStream stream = File.OpenWrite(jar)) {
                    stream.Write(code, 0, code.Length);
                    stream.Close();
                }
                ProcessStartInfo info = new ProcessStartInfo();
                if (exe.EndsWith(".shell.exe")) {
                    info.Arguments = "/c title Effyiex Java Packager & java -jar \"" + jar + "\"" + parsethrough + " & pause";
                    info.FileName = "cmd.exe";
                } else {
                    info.Arguments = parsethrough.Substring(1);
                    info.FileName = jar;
                }
                info.WorkingDirectory = cwd;
                Process.Start(info).WaitForExit();
                File.Delete(jar);
            } else {
                short jcount = 0;
                foreach (string f in Directory.GetFiles(cwd))
                    if (f.EndsWith(".jar")) {
                        jar = f;
                        jcount++;
                    }
                if (jcount > 1 && gui) {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.Title = "There are multiple Jars to choose from.";
                    dialog.InitialDirectory = cwd;
                    dialog.Filter = "Java Package (*.jar) | *.jar";
                    dialog.Multiselect = false;
                    if (dialog.ShowDialog() == DialogResult.OK) jar = dialog.FileName;
                    else Environment.Exit(0);
                }
                if (!File.Exists(jar)) {
                    string msg = "There has to be placed a jar-file in the same folder as the packager!";
                    MessageBox.Show(msg, "ERROR: jar not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                } else if(gui) {
                    string djar = jar.Substring(jar.LastIndexOf('\\') + 1).Replace(".jar", string.Empty);
                    string msg = "It'll pack the jar '" + djar + "' into a Windows-Executable and delete it afterwards";
                    DialogResult result = MessageBox.Show(msg, "INFO: Are u sure to continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result.Equals(DialogResult.No)) Environment.Exit(0);
                }
                if(gui && !shell) shell = MessageBox.Show("Shell Application?", "INFO: Build Arguments", MessageBoxButtons.YesNo, MessageBoxIcon.Question).Equals(DialogResult.Yes);
                string image = jar.Replace(".jar", shell ? ".shell.exe" : ".exe");
                if (File.Exists(image)) File.Delete(image);
                File.Copy(exe, image);
                using (FileStream stream = File.OpenRead(jar)) {
                    bytes = new byte[INDICATOR.Length + stream.Length];
                    int i = 0;
                    foreach (byte b in INDICATOR) {
                        bytes[i] = b;
                        i++;
                    }
                    while (i < bytes.Length) {
                        bytes[i] = (byte) stream.ReadByte();
                        i++;
                    }
                    stream.Close();
                }
                File.Delete(jar);
                bytes = translate(bytes, OBF_SHIFT);
                using (FileStream stream = File.OpenWrite(image)) {
                    stream.Seek(0, SeekOrigin.End);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();
                }
            }
        }

    }

}

