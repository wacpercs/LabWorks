namespace LabWorksApp.Models;

public class DichotomyStep
{
    public int StepNumber { get; set; }
    public double A { get; set; }
    public double B { get; set; }
    public double X1 { get; set; }
    public double X2 { get; set; }
    public double F1 { get; set; }
    public double F2 { get; set; }
    public double IntervalLength => Math.Abs(B - A);

    public string FormattedA => A.ToString("F6");
    public string FormattedB => B.ToString("F6");
    public string FormattedX1 => double.IsNaN(X1) ? "-" : X1.ToString("F6");
    public string FormattedX2 => double.IsNaN(X2) ? "-" : X2.ToString("F6");
    public string FormattedF1 => double.IsNaN(F1) ? "-" : F1.ToString("F6");
    public string FormattedF2 => double.IsNaN(F2) ? "-" : F2.ToString("F6");
    public string FormattedLength => IntervalLength.ToString("E4");
}
