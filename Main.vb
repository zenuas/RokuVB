Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Parser
Imports Roku.Node
Imports Roku.Architecture
Imports Roku.Util.ArrayExtension


<Assembly: AssemblyVersion("0.0.*")>
Public Class Main

    Public Class Loader

        Public Overridable Property CurrentDirectory As String
        Public Overridable Property Root As New RootNode

        Public Overridable Function GetNamespace(name As String) As String

            Return name
        End Function

        Public Overridable Sub AddNode(name As String, node As Func(Of INode))

            Dim ns = Me.GetNamespace(name)
            If Me.Root.Namespaces.ContainsKey(ns) Then Return

            Me.Root.Namespaces(ns) = Nothing
            Me.Root.Namespaces(ns) = node()
        End Sub
    End Class

    Public Shared Sub Main(args() As String)

        Dim opt As New Command.Option
        Dim xs = Command.Parser.Parse(opt, args)
        Dim loader As New Loader With {.CurrentDirectory = System.IO.Directory.GetCurrentDirectory}

        If xs.Length = 0 Then

            LoadConsole(loader, System.Console.In, opt)
        Else

            For Each arg In xs

                LoadFile(loader, arg, opt)
            Next
        End If
        Compile(loader, opt)

#If DEBUG Then
        Console.WriteLine("push any key...")
        Console.ReadKey()
#End If
    End Sub

    Public Shared Sub LoadFile(loader As Loader, f As String, opt As Command.Option)

        loader.AddNode(f,
            Function()
                Using reader As New IO.StreamReader(f)

                    Return Parse(New MyLexer(reader), opt)
                End Using
            End Function)
    End Sub

    Public Shared Sub LoadConsole(loader As Loader, reader As System.IO.TextReader, opt As Command.Option)

        loader.AddNode("", Function() Parse(New MyLexer(reader), opt))
    End Sub

    Public Shared Function Parse(lex As MyLexer, opt As Command.Option) As INode

        Dim parser As New MyParser
        lex.Parser = parser
        Return Util.Errors.Logging(Function() parser.Parse(lex),
            Sub(ex As SyntaxErrorException)

                Console.WriteLine(ex.Message)
                Console.WriteLine(lex.ReadLine)
                If lex.StoreToken IsNot Nothing Then

                    Dim store = CType(lex.StoreToken, Token)
                    Console.Write("".PadLeft(store.LineColumn.Value - 1))
                    Console.WriteLine("".PadLeft(If(store.Name Is Nothing, 1, store.Name.Length), "~"c))
                End If
            End Sub)
    End Function

    Public Shared Sub Compile(loader As Loader, opt As Command.Option)

        Dim root = CreateRootNamespace("Global")

        For Each node_ In loader.Root.Namespaces.Values

            Compiler.NameResolver.ResolveName(node_)
            Compiler.Normalize.Normalization(node_)
            Compiler.Closure.Capture(node_)
            Compiler.Typing.Prototype(node_, root)
        Next

        For Each node_ In loader.Root.Namespaces.Values

            Compiler.Typing.TypeInference(node_, root)
            Compiler.Translater.Translate(node_, root)
        Next

        If opt.NodeDump IsNot Nothing Then NodeDumpGraph(opt.NodeDump, loader.Root)
        Dim arch = CreateArchitecture(opt.Architecture)
        arch.Assemble(root)
        arch.Optimize()
        arch.Emit(opt.Output)
    End Sub

    Public Shared Function CreateRootNamespace(name As String) As Manager.RkNamespace

        Return New Manager.SystemLirary With {.Name = name}
    End Function

    Public Shared Function CreateArchitecture(name As String) As IArchitecture

        Dim separate = name.LastIndexOf(":"c)
        Dim asm = If(separate < 0, System.Reflection.Assembly.GetExecutingAssembly, System.Reflection.Assembly.LoadFrom(name.Substring(0, separate)))
        Dim arch = If(separate < 0, name, name.Substring(separate + 1))

        For Each x In asm.GetTypes

            If Not x.GetInterfaces.Or(Function(inter) inter.Equals(GetType(IArchitecture))) Then Continue For
            Dim attr = CType(x.GetCustomAttribute(GetType(ArchitectureNameAttribute)), ArchitectureNameAttribute)
            If (attr IsNot Nothing AndAlso attr.Name.Equals(arch)) OrElse x.FullName.Equals(arch) Then Return CType(x.GetConstructor(New Type() {}).Invoke(New Object() {}), IArchitecture)
        Next

        Throw New Exception($"not found architecture {name}")
    End Function

    Public Shared Sub NodeDumpGraph(out As IO.StreamWriter, node As INode)

        out.WriteLine("
digraph roku {
graph [
compound = true
];
node [
shape = record,
align = left,
];")

        Dim used As New Dictionary(Of INode, Boolean)

        Util.Traverse.NodesOnce(Of Object)(
            node,
            Nothing,
            Sub(parent, ref, child, user, isfirst, next_)

                If TypeOf parent Is RootNode Then

                    out.WriteLine($"subgraph cluster_root_{child.GetHashCode} {{")
                    out.WriteLine("  style=solid;")
                    out.WriteLine("  color=black;")
                    out.WriteLine($"  label = ""{ref}"";")
                End If

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
                        args = "\l" + String.Join("\l", func.Arguments.Map(Function(arg) $"{arg.Name.Name} : {arg.Type.Name}"))
                    End If

                    out.WriteLine($"  label = ""{name} ({block.LineNumber}){args}"";")
                    out.WriteLine("  " + String.Join(" -> ", block.Statements.Map(Function(x) x.GetHashCode.ToString)))
                    out.WriteLine("}")

                ElseIf parent IsNot Nothing AndAlso
                    TypeOf parent IsNot BlockNode AndAlso
                    TypeOf child IsNot DeclareNode AndAlso
                    TypeOf parent IsNot DeclareNode Then

                    out.WriteLine($"{parent.GetHashCode} -> {child.GetHashCode} [label = ""{ref}""];")
                    used(parent) = True
                    used(child) = True
                End If

                next_(child, user)

                If TypeOf parent Is RootNode Then

                    out.WriteLine("}")
                End If
            End Sub)

        Util.Traverse.NodesOnce(Of Object)(
            node,
            Nothing,
            Sub(parent, ref, child, user, isfirst, next_)

                If isfirst AndAlso used.ContainsKey(child) Then

                    Dim name = ""
                    If child.LineNumber.HasValue Then name = $"\l( {child.LineNumber}, {child.LineColumn} )"
                    Select Case True
                        Case TypeOf child Is VariableNode : Dim v = CType(child, VariableNode) : name += $"\l{v.Name}\l`{v.Type?.Name}`"
                        Case TypeOf child Is TypeNode : Dim v = CType(child, TypeNode) : name += $"\l{v.Name}"
                        Case TypeOf child Is NumericNode : Dim v = CType(child, NumericNode) : name += $"\l{v.Numeric}"
                        Case TypeOf child Is StringNode : Dim v = CType(child, StringNode) : name += $"\l{v.String}"
                        Case TypeOf child Is FunctionNode : Dim v = CType(child, FunctionNode) : name += $"\l{v.Name}"
                        Case TypeOf child Is ExpressionNode : Dim v = CType(child, ExpressionNode) : name += $"\l{v.Operator}\l`{v.Type?.Name}`"
                        Case TypeOf child Is DeclareNode : Dim v = CType(child, DeclareNode) : name += $"\l{v.Name.Name}\l`{v.Type?.Name}`"
                        Case TypeOf child Is LetNode : Dim v = CType(child, LetNode) : name += $"\l{v.Var.Name}\l`{v.Type?.Name}`"
                        Case TypeOf child Is StructNode : Dim v = CType(child, StructNode) : name += $"\l{v.Name}\l`{v.Type?.Name}`"
                        Case TypeOf child Is FunctionCallNode : Dim v = CType(child, FunctionCallNode) : name += $"\lsub {v.Function?.ToString}"
                    End Select

                    out.WriteLine($"{child.GetHashCode} [label = ""{child.GetType.Name}{name}""]")
                End If

                next_(child, user)
            End Sub)

        out.WriteLine("}")
    End Sub

End Class
