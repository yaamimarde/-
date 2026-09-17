namespace Pharmaceutical.Core.Interfaces;

public interface IStockRepository
{
    Task<(List<StockTransactionEntity> Items, int TotalCount)> GetPagedAsync(
        string? drugId, string? transactionType, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<bool> ProcessTransactionAsync(string drugId, string transactionType, int quantity, string operatorName, string? remark);
}
