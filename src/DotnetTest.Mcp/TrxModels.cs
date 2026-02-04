using StackXML;

// ReSharper disable MemberCanBePrivate.Global

namespace DotnetTest.Mcp.Trx;

[XmlCls("TestRun")]
public partial class TrxTestRun
{
    [XmlField("id")]
    public string? Id;

    [XmlField("name")]
    public string? Name;

    [XmlField("runUser")]
    public string? RunUser;

    [XmlField("xmlns")]
    public string? Xmlns;

    [XmlBody("Times")]
    public TrxTimes? Times;

    [XmlBody("TestSettings")]
    public TrxTestSettings? TestSettings;

    [XmlBody("Results")]
    public TrxResults? Results;

    [XmlBody("TestDefinitions")]
    public TrxTestDefinitions? TestDefinitions;

    [XmlBody("TestEntries")]
    public TrxTestEntries? TestEntries;

    [XmlBody("TestLists")]
    public TrxTestLists? TestLists;

    [XmlBody("ResultSummary")]
    public TrxResultSummary? ResultSummary;
}

[XmlCls("Times")]
public partial class TrxTimes
{
    [XmlField("creation")]
    public string? Creation;

    [XmlField("queuing")]
    public string? Queuing;

    [XmlField("start")]
    public string? Start;

    [XmlField("finish")]
    public string? Finish;
}

[XmlCls("TestSettings")]
public partial class TrxTestSettings
{
    [XmlField("name")]
    public string? Name;

    [XmlField("id")]
    public string? Id;
}

[XmlCls("Results")]
public partial class TrxResults
{
    [XmlBody("UnitTestResult")]
    public List<TrxUnitTestResult> UnitTestResults = new();
}

[XmlCls("UnitTestResult")]
public partial class TrxUnitTestResult
{
    [XmlField("testName")]
    public string? TestName;

    [XmlField("outcome")]
    public string? Outcome;

    [XmlField("testType")]
    public string? TestType;

    [XmlField("testListId")]
    public string? TestListId;

    [XmlField("testId")]
    public string? TestId;

    [XmlField("executionId")]
    public string? ExecutionId;

    [XmlField("computerName")]
    public string? ComputerName;

    [XmlField("duration")]
    public string? Duration;

    [XmlField("startTime")]
    public string? StartTime;

    [XmlField("endTime")]
    public string? EndTime;

    [XmlBody("Output")]
    public TrxOutput? Output;
}

[XmlCls("Output")]
public partial class TrxOutput
{
    [XmlBody("ErrorInfo")]
    public TrxErrorInfo? ErrorInfo;

    [XmlBody("StdOut")]
    public TrxStdOut? StdOut;

    [XmlBody("StdErr")]
    public TrxStdErr? StdErr;
}

[XmlCls("ErrorInfo")]
public partial class TrxErrorInfo
{
    [XmlBody("Message")]
    public TrxMessage? Message;

    [XmlBody("StackTrace")]
    public TrxStackTrace? StackTrace;
}

[XmlCls("Message")]
public partial class TrxMessage
{
    [XmlBody]
    public string? Value;
}

[XmlCls("StackTrace")]
public partial class TrxStackTrace
{
    [XmlBody]
    public string? Value;
}

[XmlCls("StdOut")]
public partial class TrxStdOut
{
    [XmlBody]
    public string? Value;
}

[XmlCls("StdErr")]
public partial class TrxStdErr
{
    [XmlBody]
    public string? Value;
}

[XmlCls("TestDefinitions")]
public partial class TrxTestDefinitions
{
    [XmlBody("UnitTest")]
    public List<TrxUnitTest> UnitTests = new();
}

[XmlCls("UnitTest")]
public partial class TrxUnitTest
{
    [XmlField("name")]
    public string? Name;

    [XmlField("id")]
    public string? Id;

    [XmlField("storage")]
    public string? Storage;

    [XmlBody("Execution")]
    public TrxExecution? Execution;

    [XmlBody("TestMethod")]
    public TrxTestMethod? TestMethod;
}

[XmlCls("Execution")]
public partial class TrxExecution
{
    [XmlField("id")]
    public string? Id;
}

[XmlCls("TestMethod")]
public partial class TrxTestMethod
{
    [XmlField("codeBase")]
    public string? CodeBase;

    [XmlField("className")]
    public string? ClassName;

    [XmlField("name")]
    public string? Name;

    [XmlField("adapterTypeName")]
    public string? AdapterTypeName;
}

[XmlCls("TestEntries")]
public partial class TrxTestEntries
{
    [XmlBody("TestEntry")]
    public List<TrxTestEntry> TestEntries = new();
}

[XmlCls("TestEntry")]
public partial class TrxTestEntry
{
    [XmlField("testListId")]
    public string? TestListId;

    [XmlField("testId")]
    public string? TestId;

    [XmlField("executionId")]
    public string? ExecutionId;
}

[XmlCls("TestLists")]
public partial class TrxTestLists
{
    [XmlBody("TestList")]
    public List<TrxTestList> TestLists = new();
}

[XmlCls("TestList")]
public partial class TrxTestList
{
    [XmlField("name")]
    public string? Name;

    [XmlField("id")]
    public string? Id;
}

[XmlCls("ResultSummary")]
public partial class TrxResultSummary
{
    [XmlField("outcome")]
    public string? Outcome;

    [XmlBody("Counters")]
    public TrxCounters? Counters;
}

[XmlCls("Counters")]
public partial class TrxCounters
{
    [XmlField("total")]
    public int Total;

    [XmlField("executed")]
    public int Executed;

    [XmlField("passed")]
    public int Passed;

    [XmlField("failed")]
    public int Failed;

    [XmlField("error")]
    public int Error;

    [XmlField("timeout")]
    public int Timeout;

    [XmlField("aborted")]
    public int Aborted;

    [XmlField("inconclusive")]
    public int Inconclusive;

    [XmlField("passedButRunAborted")]
    public int PassedButRunAborted;

    [XmlField("notRunnable")]
    public int NotRunnable;

    [XmlField("notExecuted")]
    public int NotExecuted;

    [XmlField("disconnected")]
    public int Disconnected;

    [XmlField("warning")]
    public int Warning;

    [XmlField("completed")]
    public int Completed;

    [XmlField("inProgress")]
    public int InProgress;

    [XmlField("pending")]
    public int Pending;
}