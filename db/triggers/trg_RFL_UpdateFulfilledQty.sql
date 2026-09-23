-- 2. تريكر تحديث الكمية المصروفة في تفاصيل الطلب
-- عند ربط حركة صرف بطلب، يتم تحديث FulfilledQty في الطلب تلقائياً
CREATE   TRIGGER [dbo].[trg_RFL_UpdateFulfilledQty]
ON [dbo].[RequisitionFulfillmentLinks]
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- زيادة الكمية المصروفة عند إنشاء الرابط
    IF EXISTS (SELECT * FROM inserted)
    BEGIN
        UPDATE rd
        SET rd.FulfilledQty = rd.FulfilledQty + i.FulfilledQty
        FROM [dbo].[Requisition_Details] rd
        INNER JOIN inserted i ON rd.RequisitionDetailId = i.RequisitionDetailId;
    END

    -- إنقاص الكمية عند حذف الرابط (في حال التراجع عن الصرف)
    IF EXISTS (SELECT * FROM deleted)
    BEGIN
        UPDATE rd
        SET rd.FulfilledQty = rd.FulfilledQty - d.FulfilledQty
        FROM [dbo].[Requisition_Details] rd
        INNER JOIN deleted d ON rd.RequisitionDetailId = d.RequisitionDetailId;
    END
END
