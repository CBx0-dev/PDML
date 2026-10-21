using System.Collections;
using PDML.Compiler.Common;

namespace PDML.Compiler.Tests.Common;

public sealed class DelimitedListTests
{
    [Fact]
    public void DelimitedList_SingleType_EnumeratesItemsOnly()
    {
        DelimitedList<string> list = new DelimitedList<string>();
        list.AddItem("a");
        list.AddDelimiter(",");
        list.AddItem("b");
        list.AddDelimiter(",");
        list.AddItem("c");

        Assert.Equal(3, list.Count);
        Assert.Equal(5, list.TotalCount);
        Assert.Equal(new[] { "a", "b", "c" }, list.ToArray());

        List<string> enumerated = new List<string>();
        IEnumerator nonGeneric = ((IEnumerable)list).GetEnumerator();
        while (nonGeneric.MoveNext())
        {
            enumerated.Add((string)nonGeneric.Current);
        }

        Assert.Equal(new[] { "a", "b", "c" }, enumerated);
    }

    [Fact]
    public void DelimitedList_TwoTypes_EnumeratesItemsOnly()
    {
        DelimitedList<int, string> list = new DelimitedList<int, string>();
        list.AddItem(10);
        list.AddDelimiter("and");
        list.AddItem(20);
        list.AddDelimiter("and");
        list.AddItem(30);

        Assert.Equal(3, list.Count);
        Assert.Equal(5, list.TotalCount);
        Assert.Equal(new[] { 10, 20, 30 }, list.ToArray());

        List<int> enumerated = new List<int>();
        IEnumerator nonGeneric = ((IEnumerable)list).GetEnumerator();
        while (nonGeneric.MoveNext())
        {
            enumerated.Add((int)nonGeneric.Current);
        }

        Assert.Equal(new[] { 10, 20, 30 }, enumerated);
    }

    [Fact]
    public void DelimitedList_Empty_EnumeratesNothing()
    {
        DelimitedList<string> single = new DelimitedList<string>();
        Assert.Empty(single);
        Assert.Equal(0, single.Count);
        Assert.Equal(0, single.TotalCount);

        DelimitedList<int, string> dual = new DelimitedList<int, string>();
        Assert.Empty(dual);
        Assert.Equal(0, dual.Count);
        Assert.Equal(0, dual.TotalCount);
    }

    [Fact]
    public void DelimitedList_SingleType_ThrowsOnInvalidOrdering()
    {
        DelimitedList<string> list = new DelimitedList<string>();
        Assert.Throws<InvalidOperationException>(() => list.AddDelimiter(","));

        list.AddItem("a");
        Assert.Throws<InvalidOperationException>(() => list.AddItem("b"));

        list.AddDelimiter(",");
        Assert.Throws<InvalidOperationException>(() => list.AddDelimiter(";"));
    }

    [Fact]
    public void DelimitedList_TwoTypes_ThrowsOnInvalidOrdering()
    {
        DelimitedList<int, string> list = new DelimitedList<int, string>();
        Assert.Throws<InvalidOperationException>(() => list.AddDelimiter(","));

        list.AddItem(1);
        Assert.Throws<InvalidOperationException>(() => list.AddItem(2));

        list.AddDelimiter(",");
        Assert.Throws<InvalidOperationException>(() => list.AddDelimiter(";"));
    }
}
