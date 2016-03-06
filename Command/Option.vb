Imports System
Imports System.Reflection


Namespace Command

    Public Class [Option]

        <CommandLine("o", "output")>
        Public Overridable Property Output As String = "a.exe"

        <CommandLine("a", "arch")>
        Public Overridable Property Architecture As String = "CIL"

        <CommandLine("N", "node-dump")>
        Public Overridable Property NodeDump As IO.TextWriter = Nothing

        Public Overridable Sub LoadPath(path As String)

        End Sub

        <CommandLine("h", "help")>
        Public Overridable Sub Help()

            Dim opt_map = Parser.GetCommand(Me)
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

    End Class

End Namespace
