namespace FinanceApp.Models;

/// <summary>
/// The months a new amount of a repeating entry applies to, counted from the month it was edited in.
/// </summary>
public enum AmountChangeScope { ThisMonth = 0, ThisAndFuture = 1, All = 2 }
