CREATE TRIGGER [dbo].[TR_TransactionDetails_AfterInsert]
ON [dbo].[Transaction_Details]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ----------------------------------------------------------------------
    -- 1) عكس أثر الصفوف القديمة (deleted) على الرصيد
    ----------------------------------------------------------------------
    ;WITH d AS
    (
        SELECT 
            d.BatchId,
            d.Quantity,
            t.TransactionType
        FROM deleted d
        INNER JOIN dbo.Transactions t ON t.TransactionId = d.TransactionId
        WHERE d.BatchId IS NOT NULL
    ),
    dAgg AS
    (
        SELECT
            BatchId,
            SUM(CASE 
                    WHEN TransactionType = 'I' THEN -Quantity  -- إدخال قديم: نرجعه (نطرح)
                    WHEN TransactionType = 'O' THEN  Quantity  -- صرف قديم: نرجعه (نضيف)
                    ELSE 0
                END) AS Delta
        FROM d
        GROUP BY BatchId
    )
    UPDATE b
    SET b.CurrentQty = b.CurrentQty + da.Delta
    FROM dbo.Batches b
    INNER JOIN dAgg da ON da.BatchId = b.BatchId;


    ----------------------------------------------------------------------
    -- 2) تطبيق أثر الصفوف الجديدة (inserted) على الرصيد
    ----------------------------------------------------------------------
    ;WITH i AS
    (
        SELECT 
            i.BatchId,
            i.Quantity,
            t.TransactionType
        FROM inserted i
        INNER JOIN dbo.Transactions t ON t.TransactionId = i.TransactionId
        WHERE i.BatchId IS NOT NULL
    ),
    iAgg AS
    (
        SELECT
            BatchId,
            SUM(CASE 
                    WHEN TransactionType = 'I' THEN  Quantity  -- إدخال جديد: نضيف
                    WHEN TransactionType = 'O' THEN -Quantity  -- صرف جديد: نطرح
                    ELSE 0
                END) AS Delta
        FROM i
        GROUP BY BatchId
    )
    UPDATE b
    SET b.CurrentQty = b.CurrentQty + ia.Delta
    FROM dbo.Batches b
    INNER JOIN iAgg ia ON ia.BatchId = b.BatchId;

END
