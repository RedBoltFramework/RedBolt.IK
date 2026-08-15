namespace RedBolt.Sample.Web;

using RedBolt.Engine;

internal static class SampleCatalog
{
    internal static Application Create(string applicationTypeName) => applicationTypeName switch
    {
        "RedBolt.Sample.InverseKinematics.InverseKinematicsApplication" => new global::RedBolt.Sample.InverseKinematics.InverseKinematicsApplication(),
        _ => throw new System.InvalidOperationException($"Unknown RedBolt sample application: {applicationTypeName}"),
    };
}
