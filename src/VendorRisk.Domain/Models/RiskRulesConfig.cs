namespace VendorRisk.Domain.Models;

public class RiskRulesConfig
{
    public WeightConfig Weights { get; set; } = new();
    public FinancialConfig Financial { get; set; } = new();
    public OperationalConfig Operational { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public SimilarityScaleConfig SimilarityScale { get; set; } = new();
}

public class WeightConfig
{
    public double Financial { get; set; } = 0.4;
    public double Operational { get; set; } = 0.3;
    public double Security { get; set; } = 0.3;
}

public class FinancialConfig
{
    public int LowThreshold { get; set; } = 50;
    public int HighThreshold { get; set; } = 80;
    public int MidMaxThreshold { get; set; } = 65;
    public double HighRisk { get; set; } = 0.85;
    public double LowRisk { get; set; } = 0.25;
    public double MidRisk { get; set; } = 0.6;
    public double StableRisk { get; set; } = 0.45;
}

public class OperationalConfig
{
    public double Base { get; set; } = 0.4;
    public int SlaLowThreshold { get; set; } = 95;
    public int SlaHighThreshold { get; set; } = 99;
    public double SlaPenalty { get; set; } = 0.25;
    public double SlaBonus { get; set; } = 0.1;
    public double IncidentStep { get; set; } = 0.07;
    public double IncidentMax { get; set; } = 0.3;
}

public class SecurityConfig
{
    public double Base { get; set; } = 0.35;
    public double MissingIsoPenalty { get; set; } = 0.25;
    public double PrivacyPenalty { get; set; } = 0.2;
    public double PentestPenalty { get; set; } = 0.25;
    public double NoCertPenalty { get; set; } = 0.1;
}

public class SimilarityScaleConfig
{
    public double Operational { get; set; } = 0.1;
    public double Security { get; set; } = 0.1;
    public double Compliance { get; set; } = 0.1;
}
