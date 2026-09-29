using System.Text;
namespace PosApp;

public class MainForm : Form
{
    const string PrinterName = "POS-58";   // exact Windows printer name
    const string Store = "MINI MART";
    static readonly Color Navy = Color.FromArgb(30, 41, 59), Bg = Color.FromArgb(241, 245, 249), Accent = Color.FromArgb(37, 99, 235);
    static readonly Color[] Palette = { Color.FromArgb(37,99,235), Color.FromArgb(22,163,74), Color.FromArgb(234,88,12), Color.FromArgb(147,51,234),
        Color.FromArgb(219,39,119), Color.FromArgb(13,148,136), Color.FromArgb(202,138,4) };

    readonly int userId;
    readonly List<CartItem> cart = new();
    readonly BindingSource bs = new();
    readonly TextBox scan = new();
    readonly DataGridView grid = new();
    readonly Label total = new();
    readonly FlowLayoutPanel cats = new(), tiles = new();
    readonly Panel cartPanel = new(), shop = new();
    readonly SplitContainer split = new();
    List<Product> products = new();
    string category = "All";
    bool cartLeft = true, styled;

    public MainForm(int uid)
    {
        userId = uid; Text = "Mini Mart POS"; WindowState = FormWindowState.Maximized; KeyPreview = true;
        Font = new Font("Segoe UI", 10); BackColor = Bg;

        // Top bar: title + scan box
        var top = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Navy, Padding = new Padding(14, 12, 14, 8) };
        var title = new Label { Text = "🛒 " + Store, ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Left, Width = 260, TextAlign = ContentAlignment.MiddleLeft };
        scan.Dock = DockStyle.Fill; scan.Font = new Font("Segoe UI", 18); scan.PlaceholderText = "Scan barcode…";
        var scanHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) }; scanHost.Controls.Add(scan);
        top.Controls.Add(scanHost); top.Controls.Add(title);

        // Cart: grid + big total
        grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToOrderColumns = true;
        grid.MultiSelect = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.RowHeadersVisible = false;
        grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.None; grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 42; grid.RowTemplate.Height = 44; grid.Font = new Font("Segoe UI", 12);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Navy; grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.DefaultCellStyle.SelectionBackColor = Accent; grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        bs.DataSource = cart; grid.DataSource = bs;
        grid.DataBindingComplete += (_, _) => StyleGrid();
        var totalBox = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.White, Padding = new Padding(0, 0, 16, 0) };
        total.Dock = DockStyle.Fill; total.Font = new Font("Segoe UI", 36, FontStyle.Bold); total.ForeColor = Accent; total.TextAlign = ContentAlignment.MiddleRight;
        var cap = new Label { Text = "TOTAL", Dock = DockStyle.Left, Width = 130, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleCenter };
        totalBox.Controls.Add(total); totalBox.Controls.Add(cap);
        cartPanel.Dock = DockStyle.Fill; cartPanel.Controls.Add(grid); cartPanel.Controls.Add(totalBox);

        // Shop: category chips + product tiles
        cats.Dock = DockStyle.Top; cats.AutoSize = true; cats.AutoSizeMode = AutoSizeMode.GrowAndShrink; cats.Padding = new Padding(6); cats.BackColor = Color.White;
        tiles.Dock = DockStyle.Fill; tiles.AutoScroll = true; tiles.Padding = new Padding(8); tiles.BackColor = Bg;
        shop.Dock = DockStyle.Fill; shop.Controls.Add(tiles); shop.Controls.Add(cats);

        // Draggable splitter between cart and shop
        split.Dock = DockStyle.Fill; split.SplitterWidth = 8; split.BackColor = Color.FromArgb(203, 213, 225);
        split.Panel1.Controls.Add(cartPanel); split.Panel2.Controls.Add(shop);

        // Bottom action bar
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 68, BackColor = Navy, Padding = new Padding(8, 6, 8, 4) };
        void Btn(string t, Color c, Action a)
        {
            var b = new Button { Text = t, Width = 128, Height = 52, FlatStyle = FlatStyle.Flat, BackColor = c, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(4) };
            b.FlatAppearance.BorderSize = 0; b.Click += (_, _) => { a(); scan.Focus(); }; bar.Controls.Add(b);
        }
        var green = Color.FromArgb(22, 163, 74); var slate = Color.FromArgb(71, 85, 105); var red = Color.FromArgb(220, 38, 38);
        Btn("Cash (F2)", green, () => Pay("CASH")); Btn("Card (F3)", Accent, () => Pay("CARD"));
        Btn("+ Qty (F5)", slate, () => Qty(1)); Btn("− Qty (F6)", slate, () => Qty(-1));
        Btn("Remove (Del)", red, RemoveLine); Btn("Clear (Esc)", red, Clear);
        Btn("Product (F9)", slate, EditProduct); Btn("Drawer (F10)", slate, () => Kick()); Btn("⇄ Swap (F11)", slate, Swap);

        Controls.Add(split); Controls.Add(bar); Controls.Add(top);

        scan.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true; Add(scan.Text.Trim()); scan.Clear();
        };
        KeyDown += (_, e) =>
        {
            switch (e.KeyCode)
            {
                case Keys.F2: Pay("CASH"); break; case Keys.F3: Pay("CARD"); break;
                case Keys.F5: Qty(1); break; case Keys.F6: Qty(-1); break;
                case Keys.F9: EditProduct(); break; case Keys.F10: Kick(); break; case Keys.F11: Swap(); break;
                case Keys.Delete: RemoveLine(); break; case Keys.Escape: Clear(); break;
            }
        };
        Shown += (_, _) =>
        {
            var availableWidth = split.ClientSize.Width - split.SplitterWidth;
            var minPanelWidth = Math.Min(320, availableWidth / 2);
            split.Panel1MinSize = minPanelWidth;
            split.Panel2MinSize = minPanelWidth;
            split.SplitterDistance = Math.Clamp((int)(split.Width * 0.42), minPanelWidth, availableWidth - minPanelWidth);
            LoadProducts(); Refresh2(); scan.Focus();
        };
    }

    void StyleGrid()
    {
        if (styled || grid.Columns.Count == 0) return;
        styled = true;
        grid.Columns["ProductId"]!.Visible = false; grid.Columns["Barcode"]!.Visible = false;
        var n = grid.Columns["Name"]!; n.HeaderText = "Item"; n.FillWeight = 200; n.DisplayIndex = 0;
        var q = grid.Columns["Qty"]!; q.HeaderText = "Qty"; q.FillWeight = 50; q.DisplayIndex = 1;
        var p = grid.Columns["Price"]!; p.DefaultCellStyle.Format = "N2"; p.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; p.DisplayIndex = 2;
        var l = grid.Columns["LineTotal"]!; l.HeaderText = "Amount"; l.DefaultCellStyle.Format = "N2"; l.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; l.DisplayIndex = 3;
    }

    decimal Total => cart.Sum(i => i.LineTotal);
    void Refresh2() { bs.ResetBindings(false); total.Text = Total.ToString("N2"); }

    // ---- product tiles ----
    void LoadProducts()
    {
        try { products = Db.Products(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        BuildCats(); BuildTiles();
    }
    void BuildCats()
    {
        cats.Controls.Clear();
        foreach (var n in new[] { "All" }.Concat(products.Select(p => p.Category).Distinct().OrderBy(x => x)))
        {
            var sel = n == category;
            var b = new Button { Text = n, AutoSize = true, MinimumSize = new Size(70, 38), FlatStyle = FlatStyle.Flat, Margin = new Padding(4),
                BackColor = sel ? Accent : Color.White, ForeColor = sel ? Color.White : Navy, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            b.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            b.Click += (_, _) => { category = n; BuildCats(); BuildTiles(); scan.Focus(); };
            cats.Controls.Add(b);
        }
    }
    void BuildTiles()
    {
        tiles.SuspendLayout(); tiles.Controls.Clear();
        var names = products.Select(p => p.Category).Distinct().OrderBy(x => x).ToList();
        foreach (var p in products.Where(p => category == "All" || p.Category == category))
        {
            var out0 = p.Stock <= 0;
            var b = new Button { Text = $"{p.Name}\n{p.Price:N2}" + (out0 ? "  (out)" : ""), Width = 140, Height = 86, Margin = new Padding(5), FlatStyle = FlatStyle.Flat,
                BackColor = out0 ? Color.Gray : Palette[names.IndexOf(p.Category) % Palette.Length], ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (_, _) => { AddProduct(p); scan.Focus(); };
            tiles.Controls.Add(b);
        }
        tiles.ResumeLayout();
    }

    // ---- cart ----
    void Add(string code)
    {
        if (code == "") return;
        try
        {
            var p = Db.Find(code);
            if (p == null) { System.Media.SystemSounds.Hand.Play(); MessageBox.Show("Unknown barcode: " + code); return; }
            AddProduct(p);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }
    void AddProduct(Product p)
    {
        var line = cart.FirstOrDefault(i => i.ProductId == p.Id);
        if ((line?.Qty ?? 0) + 1 > p.Stock) { System.Media.SystemSounds.Hand.Play(); MessageBox.Show("Out of stock: " + p.Name); return; }
        if (line != null) line.Qty++;
        else cart.Add(new CartItem { ProductId = p.Id, Barcode = p.Barcode, Name = p.Name, Price = p.Price });
        Refresh2();
    }
    void Qty(int d) { if (grid.CurrentRow?.DataBoundItem is CartItem i) { i.Qty += d; if (i.Qty <= 0) cart.Remove(i); Refresh2(); } }
    void RemoveLine() { if (grid.CurrentRow?.DataBoundItem is CartItem i) { cart.Remove(i); Refresh2(); } }
    void Clear() { cart.Clear(); Refresh2(); }
    bool Kick() => RawPrinter.Send(PrinterName, RawPrinter.KickDrawer);

    // Swap cart and product panels left/right (for left- or right-handed cashiers)
    void Swap()
    {
        cartLeft = !cartLeft;
        split.Panel1.Controls.Clear(); split.Panel2.Controls.Clear();
        split.Panel1.Controls.Add(cartLeft ? cartPanel : shop); split.Panel2.Controls.Add(cartLeft ? shop : cartPanel);
        split.SplitterDistance = Math.Max(split.Panel1MinSize, split.Width - split.SplitterDistance - split.SplitterWidth);
    }

    void Pay(string method)
    {
        if (cart.Count == 0) return;
        var due = Total; var paid = due;
        if (method == "CASH")
        {
            var s = UI.Ask("Cash", $"Total {due:N2}. Amount tendered:", due.ToString("0.00"));
            if (s == null || !decimal.TryParse(s, out paid) || paid < due) { MessageBox.Show("Invalid or insufficient amount."); return; }
        }
        try
        {
            var id = Db.CompleteSale(userId, cart, due, paid, method);
            var sb = new StringBuilder();
            sb.AppendLine(Store); sb.AppendLine($"Receipt #{id}  {DateTime.Now:g}"); sb.AppendLine(new string('-', 32));
            foreach (var i in cart) sb.AppendLine($"{i.Name}\n  {i.Qty} x {i.Price:N2}".PadRight(22) + i.LineTotal.ToString("N2").PadLeft(10));
            sb.AppendLine(new string('-', 32));
            sb.AppendLine($"TOTAL {due:N2}\n{method} {paid:N2}\nCHANGE {paid - due:N2}\n\nThank you!\n\n\n");
            var data = Encoding.ASCII.GetBytes(sb.ToString()).Concat(RawPrinter.Cut);
            if (method == "CASH") data = data.Concat(RawPrinter.KickDrawer);
            if (!RawPrinter.Send(PrinterName, data.ToArray()))
                MessageBox.Show($"Sale saved, but printer '{PrinterName}' was not found.");
            if (method == "CASH") MessageBox.Show($"Change: {paid - due:N2}");
            Clear(); LoadProducts();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Sale failed"); }
    }

    void EditProduct()
    {
        var code = UI.Ask("Product", "Barcode (scan):"); if (string.IsNullOrWhiteSpace(code)) return;
        var e = Db.Find(code.Trim());
        var name = UI.Ask("Product", "Name:", e?.Name ?? ""); if (name == null) return;
        var cat = UI.Ask("Product", "Category:", e?.Category ?? "General"); if (cat == null) return;
        var price = UI.Ask("Product", "Price:", e?.Price.ToString("0.00") ?? "0"); if (price == null) return;
        var stock = UI.Ask("Product", "Stock quantity:", e?.Stock.ToString() ?? "0"); if (stock == null) return;
        if (!decimal.TryParse(price, out var pr) || !int.TryParse(stock, out var st)) { MessageBox.Show("Invalid number."); return; }
        Db.SaveProduct(new Product(0, code.Trim(), name, pr, st, cat));
        LoadProducts();
    }
}
