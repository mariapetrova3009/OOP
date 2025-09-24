namespace VendingApp;

public class Product
{
    public int Id { get; }
    public string Name { get; }
    public int PriceRub { get; }
    public int Count { get; set; }

    public Product(int id, string name, int priceRub, int count)
    {
        Id = id;
        Name = name;
        PriceRub = priceRub;
        Count = count;
    }
}

