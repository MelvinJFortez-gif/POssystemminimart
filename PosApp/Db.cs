using MySqlConnector;
using System.Security.Cryptography;
namespace PosApp;

public record Product(int Id, string Barcode, string Name, decimal Price, int Stock, string Category = "General");
public class CartItem
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Qty { get; set; } = 1;
    public decimal LineTotal => Price * Qty;
}

public static class Db
{
    // XAMPP default: user root, empty password. Change if you set one.
    public const string ConnStr = "Server=localhost;Port=3306;Database=pos_db;User ID=root;Password=;";
    static MySqlConnection Open() { var c = new MySqlConnection(ConnStr); c.Open(); return c; }

    static string Hash(string pw)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var h = Rfc2898DeriveBytes.Pbkdf2(pw, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(h);
    }
    static bool Verify(string pw, string stored)
    {
        var p = stored.Split(':');
        var h = Rfc2898DeriveBytes.Pbkdf2(pw, Convert.FromBase64String(p[0]), 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(h, Convert.FromBase64String(p[1]));
    }

    // Creates first admin (admin / admin123) if no users exist. Change the password after first login.
    public static void Init()
    {
        using var c = Open();
        new MySqlCommand("ALTER TABLE products ADD COLUMN IF NOT EXISTS category VARCHAR(50) NOT NULL DEFAULT 'General'", c).ExecuteNonQuery();
        if (Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM users", c).ExecuteScalar()) > 0) return;
        var cmd = new MySqlCommand("INSERT INTO users(username,password_hash,role) VALUES('admin',@h,'admin')", c);
        cmd.Parameters.AddWithValue("@h", Hash("admin123"));
        cmd.ExecuteNonQuery();
    }

    public static int? Login(string user, string pw)
    {
        using var c = Open();
        var cmd = new MySqlCommand("SELECT id,password_hash FROM users WHERE username=@u", c);
        cmd.Parameters.AddWithValue("@u", user);
        using var r = cmd.ExecuteReader();
        return r.Read() && Verify(pw, r.GetString(1)) ? r.GetInt32(0) : null;
    }

    public static Product? Find(string barcode)
    {
        using var c = Open();
        var cmd = new MySqlCommand("SELECT id,barcode,name,price,stock,category FROM products WHERE barcode=@b", c);
        cmd.Parameters.AddWithValue("@b", barcode);
        using var r = cmd.ExecuteReader();
        return r.Read() ? new Product(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetInt32(4), r.GetString(5)) : null;
    }

    public static List<Product> Products()
    {
        var list = new List<Product>();
        using var c = Open();
        using var r = new MySqlCommand("SELECT id,barcode,name,price,stock,category FROM products ORDER BY category,name", c).ExecuteReader();
        while (r.Read()) list.Add(new Product(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetInt32(4), r.GetString(5)));
        return list;
    }

    public static void SaveProduct(Product p)
    {
        using var c = Open();
        var cmd = new MySqlCommand(@"INSERT INTO products(barcode,name,price,stock,category) VALUES(@b,@n,@p,@s,@cat)
            ON DUPLICATE KEY UPDATE name=VALUES(name),price=VALUES(price),stock=VALUES(stock),category=VALUES(category)", c);
        cmd.Parameters.AddWithValue("@b", p.Barcode); cmd.Parameters.AddWithValue("@n", p.Name);
        cmd.Parameters.AddWithValue("@p", p.Price); cmd.Parameters.AddWithValue("@s", p.Stock); cmd.Parameters.AddWithValue("@cat", p.Category);
        cmd.ExecuteNonQuery();
    }

    // Atomic: sale header + lines + stock deduction, or nothing.
    public static long CompleteSale(int userId, List<CartItem> items, decimal total, decimal paid, string method)
    {
        using var c = Open();
        using var tx = c.BeginTransaction();
        try
        {
            var h = new MySqlCommand("INSERT INTO sales(user_id,total,paid,change_due,method) VALUES(@u,@t,@p,@c,@m)", c, tx);
            h.Parameters.AddWithValue("@u", userId); h.Parameters.AddWithValue("@t", total);
            h.Parameters.AddWithValue("@p", paid); h.Parameters.AddWithValue("@c", paid - total); h.Parameters.AddWithValue("@m", method);
            h.ExecuteNonQuery();
            var saleId = h.LastInsertedId;
            foreach (var i in items)
            {
                var s = new MySqlCommand("UPDATE products SET stock=stock-@q WHERE id=@id AND stock>=@q", c, tx);
                s.Parameters.AddWithValue("@q", i.Qty); s.Parameters.AddWithValue("@id", i.ProductId);
                if (s.ExecuteNonQuery() == 0) throw new InvalidOperationException($"Insufficient stock: {i.Name}");
                var l = new MySqlCommand("INSERT INTO sale_items(sale_id,product_id,qty,price) VALUES(@s,@p,@q,@pr)", c, tx);
                l.Parameters.AddWithValue("@s", saleId); l.Parameters.AddWithValue("@p", i.ProductId);
                l.Parameters.AddWithValue("@q", i.Qty); l.Parameters.AddWithValue("@pr", i.Price);
                l.ExecuteNonQuery();
            }
            tx.Commit();
            return saleId;
        }
        catch { tx.Rollback(); throw; }
    }
}
