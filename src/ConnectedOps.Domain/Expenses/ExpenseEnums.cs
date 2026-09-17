namespace ConnectedOps.Domain.Expenses;

public enum ExpenseCategory
{
    Toll = 1,
    Parking = 2,
    FuelEmergency = 3,
    VehicleFluid = 4,
    TireRepair = 5,
    MealAllowance = 6,
    Other = 7
}

public enum ExpenseStatus
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Reimbursed = 4
}
