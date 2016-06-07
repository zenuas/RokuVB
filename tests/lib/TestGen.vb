' vbc TestGen.vb /t:library /optimize+
Public Class TestGen(Of Ta, Tb)

    Public Shared Function GenSharedFunction(Of Tc)(a As Ta, b As Tb, c As Tc) As Tc

        Console.WriteLine(a)
        Console.WriteLine(b)
        Console.WriteLine(c)
        Return c
    End Function

    Public Overridable Function GenMethod(Of Tc)(a As Ta, b As Tb, c As Tc) As Tc

        Console.WriteLine(a)
        Console.WriteLine(b)
        Console.WriteLine(c)
        Return c
    End Function

    Public Overridable Function NotGenMethod(a As Ta, b As Tb) As Tb

        Console.WriteLine(a)
        Console.WriteLine(b)
        Return b
    End Function
End Class
