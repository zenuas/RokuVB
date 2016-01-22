Namespace Parser

    Public Enum SymbolTypes As Integer

        ''' <summary>'('</summary>
        __x28 = 0

        ''' <summary>')'</summary>
        __x29

        ''' <summary>','</summary>
        __x2C

        ''' <summary>'.'</summary>
        __x2E

        ''' <summary>':'</summary>
        __x3A

        ''' <summary>'?'</summary>
        __x3F

        ''' <summary>'['</summary>
        __x5B

        ''' <summary>']'</summary>
        __x5D

        ''' <summary>ATVAR</summary>
        ATVAR

        ''' <summary>BEGIN</summary>
        BEGIN

        ''' <summary>ELSE</summary>
        [ELSE]

        ''' <summary>END</summary>
        [END]

        ''' <summary>EOL</summary>
        EOL

        ''' <summary>EQ</summary>
        EQ

        ''' <summary>IF</summary>
        [IF]

        ''' <summary>LET</summary>
        [LET]

        ''' <summary>NUM</summary>
        NUM

        ''' <summary>OPE</summary>
        OPE

        ''' <summary>STR</summary>
        STR

        ''' <summary>STRUCT</summary>
        STRUCT

        ''' <summary>SUB</summary>
        [SUB]

        ''' <summary>VAR</summary>
        VAR

        ''' <summary>argn</summary>
        argn

        ''' <summary>args</summary>
        args

        ''' <summary>atvar</summary>
        atvar_1

        ''' <summary>begin</summary>
        begin_1

        ''' <summary>block</summary>
        block

        ''' <summary>call</summary>
        [call]

        ''' <summary>decla</summary>
        decla

        ''' <summary>define</summary>
        define

        ''' <summary>elseif</summary>
        [elseif]

        ''' <summary>expr</summary>
        expr

        ''' <summary>extra</summary>
        extra

        ''' <summary>if</summary>
        if_1

        ''' <summary>ifthen</summary>
        ifthen

        ''' <summary>let</summary>
        let_1

        ''' <summary>line</summary>
        line

        ''' <summary>list</summary>
        list

        ''' <summary>listn</summary>
        listn

        ''' <summary>num</summary>
        num_1

        ''' <summary>start</summary>
        start

        ''' <summary>stmt</summary>
        stmt

        ''' <summary>str</summary>
        str_1

        ''' <summary>struct</summary>
        struct_1

        ''' <summary>struct_begin</summary>
        struct_begin

        ''' <summary>struct_block</summary>
        struct_block

        ''' <summary>sub</summary>
        sub_1

        ''' <summary>type</summary>
        type

        ''' <summary>typex</summary>
        typex

        ''' <summary>var</summary>
        var_1

        ''' <summary>varx</summary>
        varx

        ''' <summary>void</summary>
        void

        ''' <summary>$END</summary>
        _END

        ''' <summary>$ACCEPT</summary>
        _ACCEPT

    End Enum

End Namespace
