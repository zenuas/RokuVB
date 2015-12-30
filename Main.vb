Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Parser
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

        Compiler.NameResolver.ResolveName(node)
        Compiler.Normalize.Normalization(node)
        Compiler.Closure.Capture(node)
        Dim root = CreateRootNamespace("Global")
        Compiler.Typing.Prototype(node, root)
        Compiler.Typing.TypeInference(node, root)
        Compiler.Translater.Translate(node, root)
        If Me.NodeDump IsNot Nothing Then Me.NodeDumpGraph(Me.NodeDump, node)

    End Sub

    Public Overridable Function CreateRootNamespace(name As String) As Manager.RkNamespace

        Return New Manager.SystemLirary With {.Name = name}
    End Function

    Public Overridable Sub NodeDumpGraph(out As IO.StreamWriter, node As INode)

        out.WriteLine("
digraph roku {
graph [
];
node [
shape = record,
align = left,
];")

        Util.Traverse.NodesOnce(Of Object)(
            node,
            Nothing,
            Function(parent, ref, child, user, isfirst, next_)

                If TypeOf child Is BlockNode Then

                    Dim block = CType(child, BlockNode)
                    out.WriteLine($"subgraph cluster_block_stmt_{block.GetHashCode} {{")
                    out.WriteLine("  style=solid;")
                    out.WriteLine("  color=black;")
                    out.WriteLine("  node [style=filled];")

                    Dim name = "block"
                    Dim args = ""
                    If parent Is Nothing Then

                        name = "entrypoint"

                    ElseIf TypeOf parent Is FunctionNode AndAlso ref.Equals("Body") Then

                        Dim func = CType(parent, FunctionNode)
                        name = func.Name
                        args = "\l" + String.Join("\l", Util.Functions.Map(func.Arguments, Function(arg) $"{arg.Name.Name} : {arg.Type.Name}"))
                    End If

                    out.WriteLine($"  label = ""{name} ({block.LineNumber}){args}"";")
                    out.WriteLine("  " + String.Join(" -> ", Util.Functions.Map(block.Statements, Function(x) x.GetHashCode.ToString)))
                    out.WriteLine("}")
                End If

                next_(child, user)
                Return child
            End Function)

        Util.Traverse.NodesOnce(Of Object)(
            node,
            Nothing,
            Function(parent, ref, child, user, isfirst, next_)

                If TypeOf child Is BlockNode OrElse
                    TypeOf parent Is FunctionNode OrElse
                    TypeOf child Is FunctionNode Then

                    ' nothing
                Else

                    If isfirst Then

                        Dim name = ""
                        If child.LineNumber.HasValue Then name = $"\l( {child.LineNumber}, {child.LineColumn} )"
                        Select Case True
                            Case TypeOf child Is VariableNode : Dim v = CType(child, VariableNode) : name += $"\l{v.Name}\l`{v.Type?.Name}`"
                            Case TypeOf child Is TypeNode : Dim v = CType(child, TypeNode) : name += $"\l{v.Name}"
                            Case TypeOf child Is NumericNode : Dim v = CType(child, NumericNode) : name += $"\l{v.Numeric}"
                            Case TypeOf child Is StringNode : Dim v = CType(child, StringNode) : name += $"\l""{v.String}"""
                            Case TypeOf child Is FunctionNode : Dim v = CType(child, FunctionNode) : name += $"\l{v.Name}"
                            Case TypeOf child Is ExpressionNode : Dim v = CType(child, ExpressionNode) : name += $"\l{v.Operator}\l`{v.Type?.Name}`"
                            Case TypeOf child Is DeclareNode : Dim v = CType(child, DeclareNode) : name += $"\l{v.Name.Name}\l`{v.Type?.Name}`"
                            Case TypeOf child Is LetNode : Dim v = CType(child, LetNode) : name += $"\l{v.Var.Name}\l`{v.Type?.Name}`"
                            Case TypeOf child Is StructNode : Dim v = CType(child, StructNode) : name += $"\l{v.Name}\l`{v.Type?.Name}`"
                        End Select

                        out.WriteLine($"{child.GetHashCode} [label = ""{child.GetType.Name}{name}""]")
                    End If

                    If parent IsNot Nothing AndAlso TypeOf parent IsNot BlockNode Then out.WriteLine($"{parent.GetHashCode} -> {child.GetHashCode} [label = ""{ref}""];")
                End If

                next_(child, user)
                Return child
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

                Console.WriteLine($"  --{key}")
            Else
                Console.WriteLine($"  -{key}, --{method.Name}")
            End If
        Next

        System.Environment.Exit(0)
    End Sub

    <CommandLine("V", "version")>
    Public Overridable Sub Version()

        Console.WriteLine($"roku {Assembly.GetExecutingAssembly.GetName.Version}")

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
                If find IsNot Nothing Then Throw New Exception($"cannot interpret ``{name}''")
                find = key
            End If
        Next

        If find IsNot Nothing Then Return opts(find)

        Throw New Exception($"unknown option ``{name}''")
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
