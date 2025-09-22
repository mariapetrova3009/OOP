using System;
using System.Collections.Generic;
using System.Linq;

namespace VendingApp;

public class VendingMachine
{
    private readonly List<Product> _products = new();

    // Монетный банк автомата (после подтверждения покупок)
    private readonly Dictionary<int, int> moneybox = new()
    {
        { 10, 10 }, { 50, 10 }, { 100, 10 }, { 200, 10 }, { 500, 5 }, { 1000, 2 }
    };

    // Внесённые пользователем (ещё не подтверждённые) монеты
    private readonly Dictionary<int, int> _tray = new();

    // Внутренний баланс пользователя (остаток после покупок)
    private int _userCreditRub = 0;

    // Снимок монет, которые только что «упали» в банк при подтверждении
    private Dictionary<int, int> _lastPut = new();

    public int InsertedTotalRub => _tray.Sum(p => p.Key * p.Value);
    public int AvailableRub => _userCreditRub + InsertedTotalRub; // «Ваш баланс»
    public int CollectedRub { get; private set; }

    public readonly int[] AllowedCoins = { 10, 50, 100, 200, 500, 1000 };

    public VendingMachine()
    {
        _products.Add(new Product(1, "Вода 0.5л",    50, 5));
        _products.Add(new Product(2, "Сок яблочный", 80, 4));
        _products.Add(new Product(3, "Шоколад",      60, 6));
        _products.Add(new Product(4, "Чипсы",        70, 3));
        _products.Add(new Product(5, "Кофе",         90, 5));
    }

    // -------- пользовательские действия --------
    public void PrintProducts()
    {
        Console.WriteLine("ID  | Товар            | Цена | Остаток");
        Console.WriteLine("-----------------------------------------");
        foreach (var p in _products)
            Console.WriteLine($"{p.Id,-3} | {p.Name,-16} | {p.PriceRub,4} | {p.Count}");
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine($"Ваш баланс: {AvailableRub} рублей");
    }

    public void InsertCoin(int coinRub)
    {
        if (!AllowedCoins.Contains(coinRub))
        {
            Console.WriteLine("Такой номинал не принимается.");
            return;
        }
        if (!_tray.ContainsKey(coinRub)) _tray[coinRub] = 0;
        _tray[coinRub]++;
        Console.WriteLine($"Принято {coinRub} руб. Ваш баланс: {AvailableRub} рублей");
    }

    // Было ShowInserted()
    public void ShowCoinBank()
    {
        if (InsertedTotalRub == 0)
        {
            Console.WriteLine("Монеты не внесены.");
        }
        else
        {
            Console.WriteLine("Сейчас внесено:");
            foreach (var pair in _tray.OrderByDescending(x => x.Key))
                Console.WriteLine($"  {pair.Value} шт × {pair.Key} руб.");
        }
        Console.WriteLine($"Ваш баланс: {AvailableRub} рублей");
    }

    public void StartPurchase(int productId)
    {
        // Поиск товара по ID (без LINQ FirstOrDefault — максимально просто)
        Product product = null;
        foreach (var it in _products)
        {
            if (it.Id == productId) { product = it; break; }
        }

        if (product == null) { Console.WriteLine("Товар не найден."); return; }
        if (product.Count <= 0) { Console.WriteLine("Товар закончился."); return; }

        if (AvailableRub < product.PriceRub)
        {
            Console.WriteLine($"Недостаточно средств. Ваш баланс: {AvailableRub} рублей, нужно {product.PriceRub}.");
            return;
        }

        Console.Write($"Подтвердить покупку «{product.Name}» за {product.PriceRub} руб? (y/n): ");
        var ans = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
        if (ans != "y")
        {
            Console.WriteLine("Покупка не подтверждена.");
            return;
        }

        // 1) Переносим внесённые монеты в банк
        MoveInsertedToBank();

        // 2) Общий бюджет = старый кредит + только что внесённые монеты
        int lastMovedSum = 0;
        foreach (var kv in _lastPut) lastMovedSum += kv.Key * kv.Value;
        int totalBefore = _userCreditRub + lastMovedSum;

        // 3) Будущий внутренний баланс (остаток) после покупки
        int newCredit = totalBefore - product.PriceRub;
        if (newCredit < 0) newCredit = 0;

        // 4) Сможем ли потом выдать эти деньги монетами?
        if (newCredit > 0 && !CanMakeChange(newCredit))
        {
            Console.WriteLine("Выдать сдачу невозможно — операция отменена.");
            ReturnCoins(_lastPut); // возвращаем то, что только что внесли
            return;
        }

        // 5) Завершаем покупку
        product.Count--;
        CollectedRub += product.PriceRub;
        _userCreditRub = newCredit;

        Console.WriteLine($"Покупка успешна. Ваш баланс: {_userCreditRub} рублей");
    }

    // «Забрать сдачу» — выдать весь текущий баланс пользователя монетами
    public void TakeMoney()
    {
        if (_userCreditRub <= 0)
        {
            Console.WriteLine("Сдачи нет.");
            return;
        }

        var pack = MakeChange(_userCreditRub);
        if (pack == null)
        {
            Console.WriteLine("Сейчас не могу выдать сдачу на всю сумму. Попробуйте позже.");
            return;
        }

        Console.WriteLine("Выдача сдачи:");
        foreach (var pair in pack.OrderByDescending(p => p.Key))
            Console.WriteLine($"  {pair.Value} шт × {pair.Key} руб.");

        _userCreditRub = 0;
        Console.WriteLine("Ваш баланс: 0 рублей");
    }

    // -------- админ --------
    public void AdminMenu()
    {
        Console.Write("Пароль: ");
        var pass = Console.ReadLine();
        if (pass != "admin") { Console.WriteLine("Неверный пароль."); return; }

        while (true)
        {
            Console.WriteLine("\n[АДМИН] 1) Пополнить товар  2) Монеты в банке  3) Забрать выручку  4) Пополнить монеты  0) Выход");
            Console.Write("Выбор: ");
            var cmd = Console.ReadLine();
            if (cmd == "0") break;

            switch (cmd)
            {
                case "1": RestockProducts(); break;
                case "2": PrintCoins(); break;
                case "3": Console.WriteLine($"Забрано: {CollectedRub} руб."); CollectedRub = 0; break;
                case "4": AdminAddCoins(); break;
                default: Console.WriteLine("Неизвестная команда."); break;
            }
        }
    }

    // -------- вспомогательные --------
    private void MoveInsertedToBank()
    {
        _lastPut = new Dictionary<int, int>();

        foreach (var pair in _tray)
        {
            if (!moneybox.ContainsKey(pair.Key)) moneybox[pair.Key] = 0;
            moneybox[pair.Key] += pair.Value;   // пополнили банк
            _lastPut[pair.Key] = pair.Value;    // запомнили состав для возможного отката
        }
        _tray.Clear(); // лоток опустел
    }

    private bool CanMakeChange(int amount)
    {
        if (amount == 0) return true;

        // Копия банка, чтобы не портить оригинал (примерка)
        var temp = new Dictionary<int, int>();
        foreach (var kv in moneybox) temp[kv.Key] = kv.Value;

        int remaining = amount;
        int[] denomsDesc = { 1000, 500, 200, 100, 50, 10 };

        foreach (int denom in denomsDesc)
        {
            if (!temp.ContainsKey(denom)) continue;

            int need = remaining / denom;
            if (need <= 0) continue;

            int available = temp[denom];
            int use = need > available ? available : need;

            remaining -= use * denom;
            if (remaining == 0) break;
        }
        return remaining == 0;
    }

    private Dictionary<int, int>? MakeChange(int amount)
    {
        if (amount == 0) return new Dictionary<int, int>();

        var result = new Dictionary<int, int>();
        int remaining = amount;
        int[] denomsDesc = { 1000, 500, 200, 100, 50, 10 };

        foreach (int denom in denomsDesc)
        {
            if (!moneybox.ContainsKey(denom)) continue;

            int need = remaining / denom;
            if (need <= 0) continue;

            int available = moneybox[denom];
            int use = need > available ? available : need;

            if (use > 0)
            {
                result[denom] = use;
                remaining -= use * denom;
            }
            if (remaining == 0) break;
        }

        if (remaining == 0)
        {
            foreach (var pair in result)
                moneybox[pair.Key] -= pair.Value; // списали монеты из банка
            return result;
        }
        return null;
    }

    private void ReturnCoins(Dictionary<int, int> pack)
    {
        Console.WriteLine("Возврат внесённых средств:");
        foreach (var pair in pack.OrderByDescending(p => p.Key))
            Console.WriteLine($"  {pair.Value} шт × {pair.Key} руб.");
        // Банк не трогаем — считаем, что монеты вернули из лотка
    }

    private void RestockProducts()
    {
        PrintProducts();
        Console.Write("ID товара: ");
        if (!int.TryParse(Console.ReadLine(), out var id)) return;

        Product p = null;
        foreach (var it in _products)
        {
            if (it.Id == id) { p = it; break; }
        }
        if (p == null) { Console.WriteLine("Нет такого товара."); return; }

        Console.Write("Добавить штук: ");
        if (!int.TryParse(Console.ReadLine(), out var qty) || qty <= 0) return;
        p.Count += qty;
        Console.WriteLine("Готово.");
    }

    private void PrintCoins()
    {
        Console.WriteLine("Монеты в банке автомата (в рублях):");
        foreach (var d in moneybox.OrderByDescending(x => x.Key))
            Console.WriteLine($"  {d.Value} шт × {d.Key}");
    }

    private void AdminAddCoins()
    {
        Console.WriteLine("Доступные номиналы: 10, 50, 100, 200, 500, 1000 — в рублях");
        Console.Write("Номинал: ");
        if (!int.TryParse(Console.ReadLine(), out var denom)) return;
        if (!AllowedCoins.Contains(denom)) { Console.WriteLine("Неверный номинал."); return; }

        Console.Write("Кол-во: ");
        if (!int.TryParse(Console.ReadLine(), out var count) || count <= 0) return;

        if (!moneybox.ContainsKey(denom)) moneybox[denom] = 0;
        moneybox[denom] += count;
        Console.WriteLine("Пополнено.");
    }
}
