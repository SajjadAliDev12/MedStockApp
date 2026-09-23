-- 4. تريكر التحقق من منطق سبب الحركة (Reason Scope)
-- يمنع استخدام سبب صرف لعملية إدخال والعكس
CREATE   TRIGGER [dbo].[trg_Transactions_ValidateReasonScope]
ON [dbo].[Transactions]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 
        FROM inserted i
        INNER JOIN [dbo].[TransactionReasons] r ON i.ReasonId = r.ReasonId
        WHERE 
            -- اذا كانت الحركة وارد والسبب مخصص للصادر فقط
            (i.TransactionType = 'I' AND r.Scope = 'O')
            OR 
            -- اذا كانت الحركة صادر والسبب مخصص للوارد فقط
            (i.TransactionType = 'O' AND r.Scope = 'I')
    )
    BEGIN
        RAISERROR ('Invalid Reason Scope: The selected reason is not valid for this transaction type.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
