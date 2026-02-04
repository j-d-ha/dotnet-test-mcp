using StackXML;

// ReSharper disable MemberCanBePrivate.Global

namespace DotnetTest.Mcp.Trx;

[XmlCls("TestRun")]
public partial class TrxTestRun
{
    [XmlField("id")]
    public string? m_id;

    [XmlField("name")]
    public string? m_name;

    [XmlField("runUser")]
    public string? m_runUser;

    [XmlField("xmlns")]
    public string? m_xmlns;

    [XmlBody("Times")]
    public TrxTimes? m_times;

    [XmlBody("TestSettings")]
    public TrxTestSettings? m_testSettings;

    [XmlBody("Results")]
    public TrxResults? m_results;

    [XmlBody("TestDefinitions")]
    public TrxTestDefinitions? m_testDefinitions;

    [XmlBody("TestEntries")]
    public TrxTestEntries? m_testEntries;

    [XmlBody("TestLists")]
    public TrxTestLists? m_testLists;

    [XmlBody("ResultSummary")]
    public TrxResultSummary? m_resultSummary;
}

[XmlCls("Times")]
public partial class TrxTimes
{
    [XmlField("creation")]
    public string? m_creation;

    [XmlField("queuing")]
    public string? m_queuing;

    [XmlField("start")]
    public string? m_start;

    [XmlField("finish")]
    public string? m_finish;
}

[XmlCls("TestSettings")]
public partial class TrxTestSettings
{
    [XmlField("name")]
    public string? m_name;

    [XmlField("id")]
    public string? m_id;
}

[XmlCls("Results")]
public partial class TrxResults
{
    [XmlBody("UnitTestResult")]
    public List<TrxUnitTestResult> m_unitTestResults = new();
}

[XmlCls("UnitTestResult")]
public partial class TrxUnitTestResult
{
    [XmlField("testName")]
    public string? m_testName;

    [XmlField("outcome")]
    public string? m_outcome;

    [XmlField("testType")]
    public string? m_testType;

    [XmlField("testListId")]
    public string? m_testListId;

    [XmlField("testId")]
    public string? m_testId;

    [XmlField("executionId")]
    public string? m_executionId;

    [XmlField("computerName")]
    public string? m_computerName;

    [XmlField("duration")]
    public string? m_duration;

    [XmlField("startTime")]
    public string? m_startTime;

    [XmlField("endTime")]
    public string? m_endTime;

    [XmlBody("Output")]
    public TrxOutput? m_output;
}

[XmlCls("Output")]
public partial class TrxOutput
{
    [XmlBody("ErrorInfo")]
    public TrxErrorInfo? m_errorInfo;

    [XmlBody("StdOut")]
    public TrxStdOut? m_stdOut;

    [XmlBody("StdErr")]
    public TrxStdErr? m_stdErr;
}

[XmlCls("ErrorInfo")]
public partial class TrxErrorInfo
{
    [XmlBody("Message")]
    public TrxMessage? m_message;

    [XmlBody("StackTrace")]
    public TrxStackTrace? m_stackTrace;
}

[XmlCls("Message")]
public partial class TrxMessage
{
    [XmlBody]
    public string? m_value;
}

[XmlCls("StackTrace")]
public partial class TrxStackTrace
{
    [XmlBody]
    public string? m_value;
}

[XmlCls("StdOut")]
public partial class TrxStdOut
{
    [XmlBody]
    public string? m_value;
}

[XmlCls("StdErr")]
public partial class TrxStdErr
{
    [XmlBody]
    public string? m_value;
}

[XmlCls("TestDefinitions")]
public partial class TrxTestDefinitions
{
    [XmlBody("UnitTest")]
    public List<TrxUnitTest> m_unitTests = new();
}

[XmlCls("UnitTest")]
public partial class TrxUnitTest
{
    [XmlField("name")]
    public string? m_name;

    [XmlField("id")]
    public string? m_id;

    [XmlField("storage")]
    public string? m_storage;

    [XmlBody("Execution")]
    public TrxExecution? m_execution;

    [XmlBody("TestMethod")]
    public TrxTestMethod? m_testMethod;
}

[XmlCls("Execution")]
public partial class TrxExecution
{
    [XmlField("id")]
    public string? m_id;
}

[XmlCls("TestMethod")]
public partial class TrxTestMethod
{
    [XmlField("codeBase")]
    public string? m_codeBase;

    [XmlField("className")]
    public string? m_className;

    [XmlField("name")]
    public string? m_name;

    [XmlField("adapterTypeName")]
    public string? m_adapterTypeName;
}

[XmlCls("TestEntries")]
public partial class TrxTestEntries
{
    [XmlBody("TestEntry")]
    public List<TrxTestEntry> m_testEntries = new();
}

[XmlCls("TestEntry")]
public partial class TrxTestEntry
{
    [XmlField("testListId")]
    public string? m_testListId;

    [XmlField("testId")]
    public string? m_testId;

    [XmlField("executionId")]
    public string? m_executionId;
}

[XmlCls("TestLists")]
public partial class TrxTestLists
{
    [XmlBody("TestList")]
    public List<TrxTestList> m_testLists = new();
}

[XmlCls("TestList")]
public partial class TrxTestList
{
    [XmlField("name")]
    public string? m_name;

    [XmlField("id")]
    public string? m_id;
}

[XmlCls("ResultSummary")]
public partial class TrxResultSummary
{
    [XmlField("outcome")]
    public string? m_outcome;

    [XmlBody("Counters")]
    public TrxCounters? m_counters;
}

[XmlCls("Counters")]
public partial class TrxCounters
{
    [XmlField("total")]
    public int m_total;

    [XmlField("executed")]
    public int m_executed;

    [XmlField("passed")]
    public int m_passed;

    [XmlField("failed")]
    public int m_failed;

    [XmlField("error")]
    public int m_error;

    [XmlField("timeout")]
    public int m_timeout;

    [XmlField("aborted")]
    public int m_aborted;

    [XmlField("inconclusive")]
    public int m_inconclusive;

    [XmlField("passedButRunAborted")]
    public int m_passedButRunAborted;

    [XmlField("notRunnable")]
    public int m_notRunnable;

    [XmlField("notExecuted")]
    public int m_notExecuted;

    [XmlField("disconnected")]
    public int m_disconnected;

    [XmlField("warning")]
    public int m_warning;

    [XmlField("completed")]
    public int m_completed;

    [XmlField("inProgress")]
    public int m_inProgress;

    [XmlField("pending")]
    public int m_pending;
}