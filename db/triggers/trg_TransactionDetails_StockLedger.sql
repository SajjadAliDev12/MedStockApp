-- 1. تريكر تحديث رصيد الباتشات عند الإدخال أو الإخراج
-- يقوم بزيادة الرصيد إذا كانت الحركة (I) وإنقاصه إذا كانت (O)
CREATE   TRIGGER [dbo].[trg_TransactionDetails_StockLedger]
ON [dbo].[Transaction_Details]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- خصم الكميات للحركات الصادرة (O)
    UPDATE b
    SET b.CurrentQty = b.CurrentQty - i.Quantity
    FROM [dbo].[Batches] b
    INNER JOIN inserted i ON b.BatchId = i.BatchId
    INNER JOIN [dbo].[Transactions] t ON i.TransactionId = t.TransactionId
    WHERE t.TransactionType = 'O';

    -- إضافة الكميات للحركات الواردة (I)
    UPDATE b
    SET b.CurrentQty = b.CurrentQty + i.Quantity
    FROM [dbo].[Batches] b
    INNER JOIN inserted i ON b.BatchId = i.BatchId
    INNER JOIN [dbo].[Transactions] t ON i.TransactionId = t.TransactionId
    WHERE t.TransactionType = 'I';
END
