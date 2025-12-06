using System;
using System.Collections.Generic;

interface IState
{
    void SelectTicket(int price);
    void InsertMoney(int amount);
    void Cancel();
    void Dispense();
    string Name();
}

class TicketVendingMachine
{
    public IState IdleState;
    public IState WaitingState;
    public IState MoneyReceivedState;
    public IState TicketDispensedState;
    public IState TransactionCanceledState;
    public IState Current;
    int price;
    int balance;
    public TicketVendingMachine()
    {
        IdleState = new IdleState(this);
        WaitingState = new WaitingForMoneyState(this);
        MoneyReceivedState = new MoneyReceivedState(this);
        TicketDispensedState = new TicketDispensedState(this);
        TransactionCanceledState = new TransactionCanceledState(this);
        Current = IdleState;
        price = 0;
        balance = 0;
    }
    public void SetState(IState s) { Current = s; }
    public IState GetIdle() { return IdleState; }
    public IState GetWaiting() { return WaitingState; }
    public IState GetMoneyReceived() { return MoneyReceivedState; }
    public IState GetTicketDispensed() { return TicketDispensedState; }
    public IState GetTransactionCanceled() { return TransactionCanceledState; }
    public void SelectTicket(int p) { Current.SelectTicket(p); }
    public void InsertMoney(int amount) { Current.InsertMoney(amount); }
    public void Cancel() { Current.Cancel(); }
    public void Dispense() { Current.Dispense(); }
    public void SetPrice(int p) { price = p; }
    public int GetPrice() { return price; }
    public void AddBalance(int a) { balance += a; }
    public int GetBalance() { return balance; }
    public void ResetBalance() { balance = 0; }
}

class IdleState : IState
{
    TicketVendingMachine m;
    public IdleState(TicketVendingMachine m) { this.m = m; }
    public void SelectTicket(int price)
    {
        m.SetPrice(price);
        m.SetState(m.GetWaiting());
        Console.WriteLine("Билет выбран. Цена: " + price);
    }
    public void InsertMoney(int amount) { Console.WriteLine("Выберите билет сначала."); }
    public void Cancel() { Console.WriteLine("Нет активной транзакции."); }
    public void Dispense() { Console.WriteLine("Нечего выдавать."); }
    public string Name() { return "Idle"; }
}

class WaitingForMoneyState : IState
{
    TicketVendingMachine m;
    public WaitingForMoneyState(TicketVendingMachine m) { this.m = m; }
    public void SelectTicket(int price) { Console.WriteLine("Билет уже выбран."); }
    public void InsertMoney(int amount)
    {
        m.AddBalance(amount);
        Console.WriteLine("Внесено: " + m.GetBalance() + "/" + m.GetPrice());
        if (m.GetBalance() >= m.GetPrice())
        {
            m.SetState(m.GetMoneyReceived());
            Console.WriteLine("Достаточно средств.");
        }
    }
    public void Cancel()
    {
        m.SetState(m.GetTransactionCanceled());
        m.GetTransactionCanceled().Cancel();
    }
    public void Dispense() { Console.WriteLine("Недостаточно средств."); }
    public string Name() { return "WaitingForMoney"; }
}

class MoneyReceivedState : IState
{
    TicketVendingMachine m;
    public MoneyReceivedState(TicketVendingMachine m) { this.m = m; }
    public void SelectTicket(int price) { Console.WriteLine("Транзакция в процессе."); }
    public void InsertMoney(int amount)
    {
        m.AddBalance(amount);
        Console.WriteLine("Внесено: " + m.GetBalance() + "/" + m.GetPrice());
    }
    public void Cancel()
    {
        m.SetState(m.GetTransactionCanceled());
        m.GetTransactionCanceled().Cancel();
    }
    public void Dispense()
    {
        m.SetState(m.GetTicketDispensed());
        m.GetTicketDispensed().Dispense();
    }
    public string Name() { return "MoneyReceived"; }
}

class TicketDispensedState : IState
{
    TicketVendingMachine m;
    public TicketDispensedState(TicketVendingMachine m) { this.m = m; }
    public void SelectTicket(int price) { Console.WriteLine("Подождите, выдача..."); }
    public void InsertMoney(int amount) { Console.WriteLine("Подождите, выдача..."); }
    public void Cancel() { Console.WriteLine("Нельзя отменить, билет будет выдан."); }
    public void Dispense()
    {
        int change = m.GetBalance() - m.GetPrice();
        Console.WriteLine("Билет выдан. Сдача: " + change);
        m.ResetBalance();
        m.SetPrice(0);
        m.SetState(m.GetIdle());
    }
    public string Name() { return "TicketDispensed"; }
}

class TransactionCanceledState : IState
{
    TicketVendingMachine m;
    public TransactionCanceledState(TicketVendingMachine m) { this.m = m; }
    public void SelectTicket(int price) { Console.WriteLine("Транзакция отменяется, подождите."); }
    public void InsertMoney(int amount) { Console.WriteLine("Транзакция отменяется, возврат средств..."); }
    public void Cancel()
    {
        int refund = m.GetBalance();
        Console.WriteLine("Транзакция отменена. Возврат: " + refund);
        m.ResetBalance();
        m.SetPrice(0);
        m.SetState(m.GetIdle());
    }
    public void Dispense() { Console.WriteLine("Транзакция отменена."); }
    public string Name() { return "TransactionCanceled"; }
}

public class TicketVendingMachineApp
{
    public static void Main(string[] args)
    {
        TicketVendingMachine machine = new TicketVendingMachine();
        while (true)
        {
            Console.WriteLine("\nТекущее состояние: " + machine.Current.Name());
            Console.WriteLine("1 Выбрать билет\n2 Внести деньги\n3 Отмена\n4 Выдать билет (оператор)\n0 Выход");
            string cmd = Console.ReadLine()?.Trim();
            switch (cmd)
            {
                case "1":
                    Console.Write("Введите цену билета (целое): ");
                    try { int p = int.Parse(Console.ReadLine()?.Trim() ?? "0"); machine.SelectTicket(p); } catch { Console.WriteLine("Неверная цена"); }
                    break;
                case "2":
                    Console.Write("Введите сумму (целое): ");
                    try { int a = int.Parse(Console.ReadLine()?.Trim() ?? "0"); machine.InsertMoney(a); } catch { Console.WriteLine("Неверная сумма"); }
                    break;
                case "3":
                    machine.Cancel();
                    break;
                case "4":
                    machine.Dispense();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Неверная команда");
                    break;
            }
        }
    }
}
