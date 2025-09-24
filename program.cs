using System;

namespace VendingApp;

class Program
{
    static void Main()
    {
        VendingMachine machine = new VendingMachine();

        while (true)
        {
            Console.WriteLine("\n==== Вендинговый автомат ====");
            Console.WriteLine("1) Показать товары");
            Console.WriteLine("2) Вставить монету");
            Console.WriteLine("3) Купить товар");
            Console.WriteLine("4) Забрать сдачу");
            Console.WriteLine("5) Показать внесённые монеты");
            Console.WriteLine("6) Админ-режим");
            Console.WriteLine("0) Выход");
            Console.Write("Выбор: ");

            string command = (Console.ReadLine() ?? "").Trim();

            if (command == "0") break;

            if (command == "1")
            {
                machine.PrintProducts();
            }
            else if (command == "2")
            {
                Console.WriteLine("Доступные монеты: 10, 50, 100, 200, 500, 1000 — в рублях");
                Console.Write("Номинал: ");
                string txt = Console.ReadLine();
                if (int.TryParse(txt, out int denom))
                {
                    machine.InsertCoin(denom);
                }
                else
                {
                    Console.WriteLine("Введите число (номинал монеты).");
                }
            }
            else if (command == "3")
            {
                machine.PrintProducts();
                Console.Write("Введите ID товара: ");
                string txt = Console.ReadLine();
                if (int.TryParse(txt, out int productId))
                {
                    machine.StartPurchase(productId);
                }
                else
                {
                    Console.WriteLine("Введите число (ID товара).");
                }
            }
            else if (command == "4")
            {
                machine.TakeMoney();
            }
            else if (command == "5")
            {
                machine.ShowCoinBank();
            }
            else if (command == "6")
            {
                machine.AdminMenu();
            }
            else
            {
                Console.WriteLine("Неизвестная команда.");
            }
        }

        Console.WriteLine("До свидания!");
    }
}
