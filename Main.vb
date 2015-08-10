Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Compiler
Imports Roku.Node


<Assembly: AssemblyVersion("0.0.*")> 
Public Class Main

    Public Class Loader

        'Public Overridable Function AddImport(f As String) As Roku.Node.INode
        '    Return Nothing
        'End Function
    End Class

    Private loader_ As New Loader
    Public Shared Sub Main(args() As String)

        Dim self As New Main
        Dim xs = Parse(self, args)

        If xs.Length = 0 Then

            self.CompileConsole(self.loader_, System.Console.In)
        Else

            For Each arg In xs

                self.CompileFile(self.loader_, arg)
            Next
        End If

#If DEBUG Then
        Console.WriteLine("push any key...")
        Console.ReadKey()
#End If
    End Sub

    Public Overridable Sub CompileFile(loader As Loader, f As String)

        Using reader As New IO.StreamReader(f)

            Dim parser As New MyParser
            Me.Compile(parser.Parse(New MyLexer(reader) With {.Parser = parser}))
        End Using
        'Compile(loader.AddImport(f).Node)
    End Sub

    Public Overridable Sub CompileConsole(loader As Loader, reader As System.IO.TextReader)

        Dim parser As New MyParser
        Me.Compile(parser.Parse(New MyLexer(reader) With {.Parser = parser}))
        'Compile((New MyParser).Parse(loader, reader))
    End Sub

    Public Overridable Sub Compile(node As INode)

        If Me.NodeDump IsNot Nothing Then Me.NodeDumpGraph(Me.NodeDump, node)
    End Sub

    Public Overridable Sub NodeDumpGraph(out As IO.StreamWriter, node As INode)

        Dim mark As New Dictionary(Of Integer, Boolean)

        out.WriteLine("
digraph roku {
graph [
];
node [
shape = record,
align = left,
];")

        Dim dump_node =
            Sub(x As INode)

                Dim name = ""
                If x.LineNumber.HasValue Then name = String.Format("\l( {0}, {1} )", x.LineNumber, x.LineColumn)
                Select Case True
                    Case TypeOf x Is VariableNode : name += "\l" + CType(x, VariableNode).Name
                    Case TypeOf x Is TypeNode : name += "\l" + CType(x, TypeNode).Name
                    Case TypeOf x Is NumericNode : name += "\l" + CType(x, NumericNode).Numeric.ToString
                    Case TypeOf x Is StringNode : name += "\l""" + CType(x, StringNode).String + """"
                    Case TypeOf x Is FunctionNode : name += "\l" + CType(x, FunctionNode).Name
                    Case TypeOf x Is ExpressionNode : name += "\l" + CType(x, ExpressionNode).Operator
                    Case TypeOf x Is DeclareNode : name += "\l" + CType(x, DeclareNode).Name.Name
                    Case TypeOf x Is LetNode : name += "\l" + CType(x, LetNode).Var.Name
                End Select

                out.WriteLine("{0} [label = ""{1}{2}""]", x.GetHashCode, x.GetType.Name, name)
            End Sub
        dump_node(node)

        Util.Traverse.Nodes(node,
            Function(parent, ref, child)

                Dim child_hash = child.GetHashCode
                out.WriteLine("{0} -> {1} [label = ""{2}""];", parent.GetHashCode, child_hash, ref)

                If mark.ContainsKey(child_hash) Then Return False

                dump_node(child)
                mark.Add(child_hash, True)
                Return True
            End Function)

        out.WriteLine("}")
    End Sub

#Region "compiler option"

    Public Class CommandLineAttribute
        Inherits Attribute

        Public Sub New()

        End Sub

        Public Sub New(short_option As String)

            Me.ShortOption = short_option
        End Sub

        Public Sub New(short_option As String, long_option_ As String)

            Me.ShortOption = short_option
            Me.LongOption = long_option_
        End Sub

        Public Sub New(short_option As String, long_option_ As String, help_message As String)

            Me.ShortOption = short_option
            Me.LongOption = long_option_
            Me.HelpMessage = help_message
        End Sub

        Public Overridable Property ShortOption As String
        Public Overridable Property LongOption As String
        Public Overridable Property HelpMessage As String

    End Class

    <CommandLine("h", "help")>
    Public Overridable Sub Help()

        Dim opt_map = GetCommand(Me)
        For Each key In opt_map.Keys

            Dim method = opt_map(key)
            If method.Name.Equals(key, StringComparison.CurrentCultureIgnoreCase) Then

                Console.WriteLine("  --{0}", key)
            Else
                Console.WriteLine("  -{0}, --{1}", key, method.Name)
            End If
        Next

        System.Environment.Exit(0)
    End Sub

    <CommandLine("V", "version")>
    Public Overridable Sub Version()

        Console.WriteLine("roku {0}", Assembly.GetExecutingAssembly.GetName.Version)

        System.Environment.Exit(0)
    End Sub

    Public Overridable Sub LoadPath(path As String)

    End Sub

    <CommandLine("o", "output")>
    Public Overridable Property Output As String = "a.exe"

    <CommandLine("r", "ir")>
    Public Overridable Property IROutput As String = ""

    <CommandLine("N", "node-dump")>
    Public Overridable Property NodeDump As IO.StreamWriter = Nothing

#End Region

#Region "command line parser"

    Public Shared Function GetCommand(receiver As Object) As Dictionary(Of String, MethodInfo)

        Dim opt_map As New Dictionary(Of String, MethodInfo)

        For Each method In receiver.GetType.GetMethods(BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance)

            Dim x = GetCommandLine(method)
            If x IsNot Nothing Then

                If Not String.IsNullOrEmpty(x.ShortOption) Then opt_map.Add(x.ShortOption, method)
                If Not String.IsNullOrEmpty(x.LongOption) Then

                    opt_map.Add(x.LongOption, method)
                Else
                    opt_map.Add(method.Name.ToLower, method)
                End If
            End If
        Next

        For Each prop In receiver.GetType.GetProperties(BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.SetProperty)

            Dim x = GetCommandLine(prop)
            Dim method = prop.GetSetMethod(True)
            If x IsNot Nothing Then

                If Not String.IsNullOrEmpty(x.ShortOption) Then opt_map.Add(x.ShortOption, method)
                If Not String.IsNullOrEmpty(x.LongOption) Then

                    opt_map.Add(x.LongOption, method)
                Else
                    opt_map.Add(method.Name.ToLower, method)
                End If
            End If
        Next

        Return opt_map
    End Function

    Public Shared Function GetCommandLine(member As MemberInfo) As CommandLineAttribute

        Dim xs = member.GetCustomAttributes(GetType(CommandLineAttribute), True)
        If xs.Length = 0 Then Return Nothing
        Return CType(xs(0), CommandLineAttribute)
    End Function

    Public Shared Function FindCommand(opts As Dictionary(Of String, MethodInfo), name As String) As MethodInfo

        name = name.ToLower

        Dim find As String = Nothing
        For Each key In opts.Keys

            If key.StartsWith(name) Then

                If key.Length = name.Length Then Return opts(name)
                If find IsNot Nothing Then Throw New Exception(String.Format("cannot interpret ``{0}''", name))
                find = key
            End If
        Next

        If find IsNot Nothing Then Return opts(find)

        Throw New Exception(String.Format("unknown option ``{0}''", name))
    End Function

    Public Shared Function Parse(receiver As Object, args() As String) As String()

        Dim opts = GetCommand(receiver)
        Dim method As MethodInfo = Nothing
        Dim method_args As New List(Of Object)
        Dim rets As New List(Of String)

        For i = 0 To args.Length - 1

            'If args(i).Length = 0 Then Continue For
            If args(i).Length > 1 AndAlso args(i).Chars(0) = "-"c Then

                Dim optname As String
                Dim argument As String = Nothing

                If args(i).Chars(1) = "-"c Then

                    optname = args(i).Substring(2)
                    If optname.IndexOf("="c) >= 0 Then

                        argument = optname.Substring(optname.IndexOf("="c) + 1)
                        optname = optname.Substring(0, optname.IndexOf("="c))
                    End If
                Else

                    If args(i).Length > 2 Then argument = args(i).Substring(2)
                    optname = args(i).Chars(1).ToString
                End If
                method = FindCommand(opts, optname)

                If argument IsNot Nothing Then

                    method_args.Add(ToArgType(argument, method.GetParameters(method_args.Count).ParameterType))
                    GoTo ExecuteOption
                End If

            ElseIf method IsNot Nothing Then

                method_args.Add(ToArgType(args(i), method.GetParameters(method_args.Count).ParameterType))
            Else

                rets.Add(args(i))
            End If

            If method IsNot Nothing AndAlso method.GetParameters.Length = method_args.Count Then

ExecuteOption:
                method.Invoke(receiver, method_args.ToArray)
                method_args.Clear()
                method = Nothing
            End If
        Next

        Return rets.ToArray
    End Function

    Public Shared Function ToArgType(o As Object, [type] As System.Type) As Object

        If _
            type Is GetType(System.IO.TextReader) OrElse
            type Is GetType(System.IO.StreamReader) Then

            If o.ToString.Equals("-") Then

                Return System.Console.In
            Else
                Return New System.IO.StreamReader(New System.IO.FileStream(o.ToString, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
            End If

        ElseIf _
            type Is GetType(System.IO.TextWriter) OrElse
            type Is GetType(System.IO.StreamWriter) Then

            If o.ToString.Equals("-") Then

                Return System.Console.Out
            Else
                Dim out As New System.IO.StreamWriter(New System.IO.FileStream(o.ToString, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Write))
                out.AutoFlush = True
                Return out
            End If

        ElseIf type.GetMethod("Parse", New System.Type() {o.GetType}) IsNot Nothing Then

            Return type.InvokeMember("Parse", BindingFlags.InvokeMethod, Nothing, Nothing, New Object() {o})
        Else
            Return o.ToString
        End If
    End Function

#End Region

End Class
