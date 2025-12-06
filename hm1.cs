using System;
using System.Collections.Generic;
using System.Linq;

public class LibrarySystem
{
    abstract class User
    {
        public Guid Id;
        public string Name;
        public string Role;
        public User(string name, string role) { Id = Guid.NewGuid(); Name = name; Role = role; }
    }
    class Reader : User
    {
        public List<Reservation> History = new List<Reservation>();
        public Reader(string name) : base(name, "Reader") { }
    }
    class Librarian : Reader
    {
        public Librarian(string name) : base(name) { Role = "Librarian"; }
    }
    class Administrator : Librarian
    {
        public Administrator(string name) : base(name) { Role = "Administrator"; }
    }
    class Book
    {
        public enum StateEnum { AVAILABLE, RESERVED, CHECKED_OUT, OVERDUE, RETURNED, LOST }
        public Guid Id;
        public string Title;
        public string Author;
        public string Genre;
        public StateEnum State;
        public Book(string t, string a, string g) { Id = Guid.NewGuid(); Title = t; Author = a; Genre = g; State = StateEnum.AVAILABLE; }
        public override string ToString() { return $"{Id.ToString().Substring(0,8)} | {Title} | {Author} | {State}"; }
    }
    class Branch
    {
        public Guid Id;
        public string Name;
        public Dictionary<Guid, Book> Books = new Dictionary<Guid, Book>();
        public Branch(string name) { Id = Guid.NewGuid(); Name = name; }
    }
    class Reservation
    {
        public Guid Id;
        public Guid BookId;
        public Guid UserId;
        public DateTime Date;
        public bool Active;
        public Reservation(Guid b, Guid u) { Id = Guid.NewGuid(); BookId = b; UserId = u; Date = DateTime.Now.Date; Active = true; }
        public override string ToString() { return $"{Id.ToString().Substring(0,8)} book:{BookId.ToString().Substring(0,8)} user:{UserId.ToString().Substring(0,8)} date:{Date.ToString("yyyy-MM-dd")}"; }
    }
    class Data
    {
        public Dictionary<Guid, User> Users = new Dictionary<Guid, User>();
        public Dictionary<Guid, Branch> Branches = new Dictionary<Guid, Branch>();
        public Dictionary<Guid, Reservation> Reservations = new Dictionary<Guid, Reservation>();
        public Dictionary<Guid, Guid> CheckedOutBy = new Dictionary<Guid, Guid>();
    }

    static Data data = new Data();

    static void Main()
    {
        seedDemo();
        while (true)
        {
            Console.WriteLine("\n1 Регистрация читателя\n2 Добавить книгу (библиотекарь/админ)\n3 Просмотр всех книг\n4 Поиск книг\n5 Бронирование книги\n6 Отмена бронирования\n7 Выдача книги\n8 Возврат книги\n9 Просмотр истории бронирований пользователя\n10 Управление филиалами (админ)\n11 Просмотр аналитики (админ)\n0 Выход");
            string c = Console.ReadLine()?.Trim();
            try
            {
                switch (c)
                {
                    case "1": register(); break;
                    case "2": addBook(); break;
                    case "3": listAllBooks(); break;
                    case "4": searchBooks(); break;
                    case "5": reserveBook(); break;
                    case "6": cancelReservation(); break;
                    case "7": issueBook(); break;
                    case "8": returnBook(); break;
                    case "9": viewUserHistory(); break;
                    case "10": manageBranches(); break;
                    case "11": analytics(); break;
                    case "0": Environment.Exit(0); break;
                    default: Console.WriteLine("Неверный выбор"); break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка: " + e.Message);
            }
        }
    }

    static void seedDemo()
    {
        var admin = new Administrator("Admin");
        data.Users[admin.Id] = admin;
        var lib = new Librarian("Ivan");
        data.Users[lib.Id] = lib;
        var r = new Reader("Petr");
        data.Users[r.Id] = r;
        var b1 = new Branch("Центральная");
        var b2 = new Branch("Филиал №1");
        data.Branches[b1.Id] = b1;
        data.Branches[b2.Id] = b2;
        addBookToBranch(b1, "Война и мир", "Толстой", "Роман");
        addBookToBranch(b1, "Преступление и наказание", "Достоевский", "Роман");
        addBookToBranch(b2, "Java. Руководство", "Шилдт", "Техническая");
    }

    static void register()
    {
        Console.Write("Имя: ");
        string name = Console.ReadLine()?.Trim() ?? "";
        var r = new Reader(name);
        data.Users[r.Id] = r;
        Console.WriteLine("Зарегистрирован: id=" + r.Id.ToString().Substring(0, 8));
    }

    static Branch chooseBranch()
    {
        if (data.Branches.Count == 0) { Console.WriteLine("Нет филиалов"); return null; }
        var list = data.Branches.Values.ToList();
        for (int i = 0; i < list.Count; i++) Console.WriteLine((i + 1) + " " + list[i].Name);
        Console.Write("Выберите филиал: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out int idx)) return null;
        idx = idx - 1;
        if (idx < 0 || idx >= list.Count) return null;
        return list[idx];
    }

    static void addBook()
    {
        Console.Write("Ваш id (библиотекарь/админ): ");
        string uid = Console.ReadLine()?.Trim() ?? "";
        var u = findUserByShortId(uid);
        if (u == null || !(u is Librarian || u is Administrator)) { Console.WriteLine("Нет прав"); return; }
        var br = chooseBranch();
        if (br == null) { Console.WriteLine("Филиал не выбран"); return; }
        Console.Write("Название: "); string t = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Автор: "); string a = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Жанр: "); string g = Console.ReadLine()?.Trim() ?? "";
        addBookToBranch(br, t, a, g);
        Console.WriteLine("Книга добавлена");
    }

    static void addBookToBranch(Branch br, string t, string a, string g)
    {
        var b = new Book(t, a, g);
        br.Books[b.Id] = b;
    }

    static void listAllBooks()
    {
        foreach (var br in data.Branches.Values)
        {
            Console.WriteLine("Филиал: " + br.Name);
            foreach (var b in br.Books.Values) Console.WriteLine(b);
        }
    }

    static void searchBooks()
    {
        Console.Write("Поиск по (title/author/genre): ");
        string f = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
        Console.Write("Запрос: ");
        string q = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
        foreach (var br in data.Branches.Values)
        {
            foreach (var b in br.Books.Values)
            {
                bool ok = false;
                if (f == "title" && b.Title.ToLowerInvariant().Contains(q)) ok = true;
                if (f == "author" && b.Author.ToLowerInvariant().Contains(q)) ok = true;
                if (f == "genre" && b.Genre.ToLowerInvariant().Contains(q)) ok = true;
                if (ok) Console.WriteLine(br.Name + " | " + b);
            }
        }
    }

    static void reserveBook()
    {
        Console.Write("Ваш id: ");
        string uid = Console.ReadLine()?.Trim() ?? "";
        var u = findUserByShortId(uid);
        if (u == null) { Console.WriteLine("Пользователь не найден"); return; }
        Console.Write("id книги (8 символов): ");
        string bid = Console.ReadLine()?.Trim() ?? "";
        var b = findBookByShortId(bid);
        if (b == null) { Console.WriteLine("Книга не найдена"); return; }
        if (b.State != Book.StateEnum.AVAILABLE) { Console.WriteLine("Книга недоступна"); return; }
        var r = new Reservation(b.Id, u.Id);
        data.Reservations[r.Id] = r;
        b.State = Book.StateEnum.RESERVED;
        if (u is Reader rr) rr.History.Add(r);
        Console.WriteLine("Забронирована. reservation id: " + r.Id.ToString().Substring(0, 8));
    }

    static void cancelReservation()
    {
        Console.Write("Ваш id: ");
        string uid = Console.ReadLine()?.Trim() ?? "";
        var u = findUserByShortId(uid); if (u == null) { Console.WriteLine("Нет"); return; }
        Console.Write("id резерва (8): ");
        string rid = Console.ReadLine()?.Trim() ?? "";
        var r = findReservationByShortId(rid);
        if (r == null || !r.Active) { Console.WriteLine("Резерв не найден или неактивен"); return; }
        r.Active = false;
        var b = findBookById(r.BookId);
        if (b != null) b.State = Book.StateEnum.AVAILABLE;
        Console.WriteLine("Бронирование отменено");
    }

    static void issueBook()
    {
        Console.Write("Ваш id (библиотекарь/админ): ");
        string uid = Console.ReadLine()?.Trim() ?? "";
        var u = findUserByShortId(uid);
        if (u == null || !(u is Librarian || u is Administrator)) { Console.WriteLine("Нет прав"); return; }
        Console.Write("id книги (8): ");
        string bid = Console.ReadLine()?.Trim() ?? "";
        var b = findBookByShortId(bid); if (b == null) { Console.WriteLine("Нет"); return; }
        Console.Write("id читателя (8): ");
        string rid = Console.ReadLine()?.Trim() ?? "";
        var user = findUserByShortId(rid); if (user == null) { Console.WriteLine("Нет"); return; }
        if (b.State == Book.StateEnum.CHECKED_OUT) { Console.WriteLine("Уже выдана"); return; }
        b.State = Book.StateEnum.CHECKED_OUT;
        data.CheckedOutBy[b.Id] = user.Id;
        Console.WriteLine("Книга выдана");
    }

    static void returnBook()
    {
        Console.Write("id книги (8): ");
        string bid = Console.ReadLine()?.Trim() ?? "";
        var b = findBookByShortId(bid); if (b == null) { Console.WriteLine("Нет"); return; }
        b.State = Book.StateEnum.RETURNED;
        data.CheckedOutBy.Remove(b.Id);
        b.State = Book.StateEnum.AVAILABLE;
        Console.WriteLine("Книга возвращена и доступна");
    }

    static void viewUserHistory()
    {
        Console.Write("id пользователя (8): ");
        string uid = Console.ReadLine()?.Trim() ?? "";
        var u = findUserByShortId(uid);
        if (u == null || !(u is Reader)) { Console.WriteLine("Нет"); return; }
        var r = (Reader)u;
        foreach (var res in r.History) Console.WriteLine(res);
    }

    static void manageBranches()
    {
        Console.Write("Ваш id (админ): ");
        string uid = Console.ReadLine()?.Trim() ?? "";
        var u = findUserByShortId(uid);
        if (u == null || !(u is Administrator)) { Console.WriteLine("Нет прав"); return; }
        Console.WriteLine("1 Добавить филиал\n2 Удалить филиал");
        string c = Console.ReadLine()?.Trim() ?? "";
        if (c == "1")
        {
            Console.Write("Название: "); string n = Console.ReadLine()?.Trim() ?? "";
            var b = new Branch(n); data.Branches[b.Id] = b; Console.WriteLine("Добавлен id:" + b.Id.ToString().Substring(0, 8));
        }
        else if (c == "2")
        {
            var list = data.Branches.Values.ToList();
            for (int i = 0; i < list.Count; i++) Console.WriteLine((i + 1) + " " + list[i].Name);
            Console.Write("Выберите: "); if (!int.TryParse(Console.ReadLine()?.Trim(), out int idx)) { Console.WriteLine("Неверно"); return; }
            idx = idx - 1;
            if (idx < 0 || idx >= list.Count) { Console.WriteLine("Неверно"); return; }
            data.Branches.Remove(list[idx].Id); Console.WriteLine("Удалён");
        }
    }

    static void analytics()
    {
        Console.Write("Ваш id (админ): ");
        string uid = Console.ReadLine()?.Trim() ?? "";
        var u = findUserByShortId(uid);
        if (u == null || !(u is Administrator)) { Console.WriteLine("Нет прав"); return; }
        int total = 0; Dictionary<string, int> popularity = new Dictionary<string, int>();
        foreach (var br in data.Branches.Values)
        {
            foreach (var b in br.Books.Values)
            {
                total++;
                int add = b.State == Book.StateEnum.CHECKED_OUT ? 1 : 0;
                popularity[b.Title] = popularity.ContainsKey(b.Title) ? popularity[b.Title] + add : add;
            }
        }
        Console.WriteLine("Всего книг: " + total);
        var list = popularity.ToList();
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        Console.WriteLine("Топ выданных книг:");
        for (int i = 0; i < Math.Min(5, list.Count); i++) Console.WriteLine((i + 1) + " " + list[i].Key + " (" + list[i].Value + ")");
    }

    static User findUserByShortId(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.ToLowerInvariant();
        foreach (var u in data.Users.Values) if (u.Id.ToString().ToLowerInvariant().StartsWith(s)) return u;
        return null;
    }

    static Book findBookByShortId(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.ToLowerInvariant();
        foreach (var br in data.Branches.Values)
        {
            foreach (var b in br.Books.Values) if (b.Id.ToString().ToLowerInvariant().StartsWith(s)) return b;
        }
        return null;
    }

    static Book findBookById(Guid id)
    {
        foreach (var br in data.Branches.Values) if (br.Books.ContainsKey(id)) return br.Books[id];
        return null;
    }

    static Reservation findReservationByShortId(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.ToLowerInvariant();
        foreach (var r in data.Reservations.Values) if (r.Id.ToString().ToLowerInvariant().StartsWith(s)) return r;
        return null;
    }
}
