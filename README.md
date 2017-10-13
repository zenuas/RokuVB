Roku [![MIT License](http://img.shields.io/badge/license-MIT-blue.svg?style=flat)](LICENSE)
====
Roku is compiler

## Description

Roku is a compiler that outputs assemblies of the .NET Framework.  
not class-based, not object-oriented.  
only global function and poor functions.  
type inference is possible but insufficient.  
not optimization, code size is very large.

Rokuは.NET Frameworkのアセンブリを出力するコンパイラである  
クラスベースではなく、オブジェクト指向でもないコンパイラである  
グローバル関数しかなく、大した機能も持っていない  
型推論はできるが不十分である  
最適化は行わず、コードサイズは非常に大きい

## Usage

`./roku sourcefile.rk`

`./roku sourcefile.rk -o output.exe`

`./roku sourcefile.rk -o output.exe -l library.dll`

## Grammar

```
# comment
var n = 100
print(n)
```

### function

```
sub add(a: Int, b: Int) Int
    return(a + b)

f(1, 2)
```

### struct

```
struct Foo
    var n = 10
    var s = "hello"

var a = Foo()
print(a.s)
```

### generic type

```
sub f(a: @T)
    print(a)

f(100)
f("hello")
```

### if branch

```
if a > 100
    print("over 100")
else if a > 50
    print("over 50")
else
    print("other")
```

### switch branch

```
switch x
    0         => print("== 0")
    1         => print("== 1")
    n: Int    => print("int")
    s: String => print("string")
    f: Foo    => print("foo")
    _         => print("else case")
```

## Licence

[MIT License](LICENSE)

## Author

[zenuas](https://github.com/zenuas)
