namespace PosApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try { Db.Init(); }
        catch (Exception ex)
        {
            MessageBox.Show("Cannot reach MySQL. Start MySQL in the XAMPP Control Panel and import database.sql.\n\n" + ex.Message);
            return;
        }
        for (int tries = 0; tries < 3; tries++)
        {
            var u = UI.Ask("Login", "Username"); if (u == null) return;
            var p = UI.Ask("Login", "Password", "", true); if (p == null) return;
            var id = Db.Login(u, p);
            if (id != null) { Application.Run(new MainForm(id.Value)); return; }
            MessageBox.Show("Invalid credentials.");
        }
    }
}

static class UI
{
    public static string? Ask(string title, string label, string def = "", bool password = false)
    {
        using var f = new Form { Text = title, Width = 320, Height = 150, StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var l = new Label { Text = label, Left = 10, Top = 10, Width = 280 };
        var t = new TextBox { Left = 10, Top = 35, Width = 280, Text = def, UseSystemPasswordChar = password };
        var ok = new Button { Text = "OK", Left = 215, Top = 70, DialogResult = DialogResult.OK };
        f.Controls.AddRange(new Control[] { l, t, ok }); f.AcceptButton = ok;
        return f.ShowDialog() == DialogResult.OK ? t.Text : null;
    }
}
