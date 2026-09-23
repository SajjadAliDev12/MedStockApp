-- 3. تريكر التحقق من تطابق المواد (Validation)
-- يمنع صرف مادة مختلفة عن المادة المطلوبة في الطلب
CREATE   TRIGGER [dbo].[trg_RFL_ValidateLink]
ON [dbo].[RequisitionFulfillmentLinks]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 
        FROM inserted i
        INNER JOIN [dbo].[Requisition_Details] rd ON i.RequisitionDetailId = rd.RequisitionDetailId
        INNER JOIN [dbo].[Transaction_Details] td ON i.TransactionDetailId = td.TransactionDetailId
        INNER JOIN [dbo].[Batches] b ON td.BatchId = b.BatchId
        WHERE rd.ItemId <> b.ItemId
    )
    BEGIN
        RAISERROR ('Item Mismatch: The item in the batch does not match the item in the requisition.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
