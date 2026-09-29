// Exact-build patcher for the user's NFS Underground (2003) executable.
// Built against .NET Framework 4.x. No third-party runtime dependencies.
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Windows.Forms;

namespace NfsuMod {
public static class Patch {
    public const string OriginalHash = "8234B86F0BCABB9854CE89E7DF52534787D923080BA34791C79E6F5040920EA8";
    public const string DefaultFolder = @"C:\Program Files (x86)\EA Games\NFS Underground";
    public const string BackupName = "speed.exe.before-nfsu-mod.bak";
    public static string Hash(byte[] data) { using(var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(data)).Replace("-", ""); }
    static byte[] Hex(string value) { return Enumerable.Range(0, value.Length / 2).Select(i => Convert.ToByte(value.Substring(i * 2, 2), 16)).ToArray(); }
    static void Change(byte[] data, int offset, string expected, string replacement) {
        byte[] old = Hex(expected), next = Hex(replacement);
        if(old.Length != next.Length || !data.Skip(offset).Take(old.Length).SequenceEqual(old)) throw new InvalidDataException("Unexpected instructions at file offset 0x" + offset.ToString("X") + ". No changes made.");
        Buffer.BlockCopy(next, 0, data, offset, next.Length);
    }
    public static byte[] Build(byte[] original, bool nitrous, bool unlock) {
        if(Hash(original) != OriginalHash) throw new InvalidDataException("Unsupported speed.exe. This mod is matched to your inspected 3,178,496-byte executable only.");
        byte[] result = (byte[])original.Clone();
        if(nitrous) {
            // Player effects update has two additional inline expiry checks.
            Change(result, 0x28E20, "2BD0", "31D2");
            Change(result, 0x28F55, "2B86BC030000", "31C090909090");
            // A third inline expiry check in the vehicle's effect update.
            Change(result, 0x5F92D, "2BD0", "31D2");
            // The cumulative-use cap otherwise clears the supply after a long boost.
            Change(result, 0x79F7A, "7A1F", "EB1F");
            // Remaining-NOS getter: use start time twice, so elapsed time is zero.
            Change(result, 0x49F91, "DB0524AD7300", "DB81BC030000");
            // Active-NOS test: elapsed = 0; keep the existing activation requirement.
            Change(result, 0x6544D, "2B86BC030000", "31C090909090");
            // Activation: do not deduct a bottle's allocation from remaining NOS.
            Change(result, 0x65524, "298AB8030000", "909090909090");
            // Release: deduct zero elapsed time, but still reset the active timer.
            Change(result, 0x6554D, "2B86BC030000", "31C090909090");
        }
        if(unlock) {
            // A 12-byte startup stub in verified zero-filled .text padding.
            // Set this build's native unlock flag, then jump to the original entry.
            Change(result, 0x296EB0, "000000000000000000000000", "C605B95E730001E9F99DFDFF");
            Change(result, 0x128, "B50C2700", "B06E2900");
            // Extend .text VirtualSize to include the stub, staying within its raw size.
            Change(result, 0x200, "A75E2900", "BC5E2900");
        }
        return result;
    }
    static bool IsGenerated(byte[] current, byte[] original) {
        for(int i=0; i<4; i++) if(Hash(Build(original, (i&1)!=0, (i&2)!=0))==Hash(current)) return true;
        return false;
    }
    public static byte[] Original(string folder) {
        string exe=Path.Combine(folder,"speed.exe"), backup=Path.Combine(folder,BackupName);
        byte[] current=File.ReadAllBytes(exe);
        if(Hash(current)==OriginalHash) return current;
        if(!File.Exists(backup)) throw new InvalidDataException("The game executable is not the supported original, and no backup from this patcher exists.");
        byte[] original=File.ReadAllBytes(backup);
        if(Hash(original)!=OriginalHash || !IsGenerated(current,original)) throw new InvalidDataException("The executable or backup was changed by something else. Refusing to overwrite it.");
        return original;
    }
    public static string Inspect(string folder) {
        byte[] original=Original(folder), current=File.ReadAllBytes(Path.Combine(folder,"speed.exe"));
        for(int i=0;i<4;i++) if(Hash(current)==Hash(Build(original,(i&1)!=0,(i&2)!=0))) return "Exact executable verified.\r\nInfinite nitrous: "+((i&1)!=0?"ON":"OFF")+"    Unlock everything: "+((i&2)!=0?"ON":"OFF");
        throw new InvalidDataException("Unrecognized file state.");
    }
    static void EnsureClosed() {
        foreach(var p in Process.GetProcessesByName("speed")) { p.Dispose(); throw new InvalidOperationException("Close NFS Underground before applying or restoring the mod."); }
    }
    public static void Install(string folder, bool nitrous, bool unlock) {
        EnsureClosed();
        folder=Path.GetFullPath(folder);
        byte[] original=Original(folder), target=Build(original,nitrous,unlock);
        string exe=Path.Combine(folder,"speed.exe"), backup=Path.Combine(folder,BackupName);
        byte[] current=File.ReadAllBytes(exe);
        if(Hash(target)==Hash(current)) return;
        if(File.Exists(backup)) {
            if(Hash(File.ReadAllBytes(backup))!=OriginalHash) throw new InvalidDataException("The existing backup does not match. It will not be overwritten.");
        } else {
            using(var f=new FileStream(backup,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {f.Write(original,0,original.Length);f.Flush(true);}
        }
        if(Hash(File.ReadAllBytes(backup))!=OriginalHash) throw new IOException("Backup verification failed; the game was not changed.");
        string temp=Path.Combine(folder,"speed.nfsumod-"+Guid.NewGuid().ToString("N")+".tmp");
        try {
            using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {f.Write(target,0,target.Length);f.Flush(true);}
            if(Hash(File.ReadAllBytes(temp))!=Hash(target)) throw new IOException("Temporary-file verification failed.");
            EnsureClosed();
            if(Hash(File.ReadAllBytes(exe))!=Hash(current)) throw new IOException("The executable changed during installation. No replacement was made.");
            File.Replace(temp,exe,null);
            if(Hash(File.ReadAllBytes(exe))!=Hash(target)) throw new IOException("Final verification failed. The original remains in "+backup);
        } finally { if(File.Exists(temp)) File.Delete(temp); }
    }
}

public class Window : Form {
    TextBox folder=new TextBox(); CheckBox nitrous=new CheckBox(), unlock=new CheckBox(); Label state=new Label();
    public Window() {
        Text="NFS Underground | Nitrous + Unlock"; ClientSize=new Size(620,405); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",10); BackColor=Color.FromArgb(19,23,30); ForeColor=Color.White;
        var heading=new Label{Text="UNDERGROUND / UNLIMITED",Location=new Point(26,23),Size=new Size(570,34),Font=new Font("Segoe UI",18,FontStyle.Bold),ForeColor=Color.FromArgb(170,239,196)}; Controls.Add(heading);
        Controls.Add(new Label{Text="For your verified NFS Underground (2003) PC executable",Location=new Point(28,64),Size=new Size(565,25)});
        folder.Text=Patch.DefaultFolder;folder.SetBounds(28,106,458,28);Controls.Add(folder);
        var browse=Button("Browse…",497,104,95);browse.Click+=(s,e)=>{using(var d=new FolderBrowserDialog()){d.Description="Choose the folder containing speed.exe";d.SelectedPath=folder.Text;if(d.ShowDialog()==DialogResult.OK){folder.Text=d.SelectedPath;Check();}}};
        nitrous.Text="Infinite nitrous (requires an equipped nitrous upgrade)";nitrous.Checked=true;nitrous.SetBounds(29,156,565,29);Controls.Add(nitrous);
        unlock.Text="Unlock all parts, cars and tracks";unlock.Checked=true;unlock.SetBounds(29,194,565,29);Controls.Add(unlock);
        state.SetBounds(28,244,565,65);state.ForeColor=Color.FromArgb(180,193,210);Controls.Add(state);
        var apply=Button("Apply selected features",28,335,220);apply.Click+=(s,e)=>Apply(nitrous.Checked,unlock.Checked);
        var restore=Button("Restore original",260,335,165);restore.Click+=(s,e)=>Apply(false,false);
        var check=Button("Check version",437,335,155);check.Click+=(s,e)=>Check();
        Shown+=(s,e)=>Check();
    }
    Button Button(string text,int x,int y,int width) {var b=new Button{Text=text,Location=new Point(x,y),Size=new Size(width,38),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(36,45,54)};Controls.Add(b);return b;}
    void Check(){try{state.Text=Patch.Inspect(folder.Text);}catch(Exception e){state.Text=e.Message;}}
    void Apply(bool n,bool u) {
        try {
            Patch.Original(folder.Text); // Validate before asking Windows for elevated access.
            bool admin=new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if(!admin) {
                var info=new ProcessStartInfo(Application.ExecutablePath,"--apply-ui "+(n?"1":"0")+" "+(u?"1":"0")+" \""+Path.GetFullPath(folder.Text)+"\""){UseShellExecute=true,Verb="runas"};
                using(var p=Process.Start(info)){p.WaitForExit();} Check();return;
            }
            Patch.Install(folder.Text,n,u);Check();MessageBox.Show(this,"Done. The original executable backup is kept in the game folder.","NFSU Mod",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }catch(Exception e){MessageBox.Show(this,e.Message,"Mod not applied",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
}

public static class Program {
    [STAThread] public static int Main(string[] args) {
        try {
            if(args.Length==0){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Window());return 0;}
            if(args[0]=="--inspect"&&args.Length==2){Console.WriteLine(Patch.Inspect(args[1]));return 0;}
            if(args[0]=="--build-copy"&&args.Length==5){byte[] result=Patch.Build(File.ReadAllBytes(args[3]),args[1]=="1",args[2]=="1");using(var f=new FileStream(args[4],FileMode.CreateNew)){f.Write(result,0,result.Length);}return 0;}
            if((args[0]=="--apply"||args[0]=="--apply-ui")&&args.Length==4){Patch.Install(args[3],args[1]=="1",args[2]=="1");if(args[0]=="--apply-ui")MessageBox.Show("Done. Your original executable backup is kept in the game folder.","NFSU Mod");return 0;}
            throw new ArgumentException("Usage: --inspect folder | --apply 1 1 folder | --apply 0 0 folder (restore)");
        }catch(Exception e){if(args.Length>0&&args[0]=="--apply-ui")MessageBox.Show(e.Message,"Mod not applied",MessageBoxButtons.OK,MessageBoxIcon.Error);else Console.Error.WriteLine(e.Message);return 1;}
    }
}
}
