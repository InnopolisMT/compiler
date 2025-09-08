# Presentation

## Team

Our team is **Tourists**
Team members are **Danila Khrankou & Tsimafei Kurstak**

## Task

Language: **ProjectI (Imperative)**
Implementation: **C#**
Tool: **gppg-based parser**
Target platform: **WASM**

## Test-cases

### Test-case 1
```pascal
var x : integer is 10
var y is 20.5
var flag : boolean is true

print x, y, flag
```

### Test-case 2
```pascal
var n : integer is 5

if n > 0 then
    print "Positive"
else
    print "Non-positive"
end
```

### Test-case 3
```pascal
var n : integer is 5

for i in 1..n loop
    print i
end
```

### Test-case 4
```pascal
var n : integer is 5

while n > 0 loop
    print n
    n := n - 1
end
```

### Test-case 5
```pascal
type IntArray is array[5] integer
var arr : IntArray is [1, 2, 3, 4, 5]

for i in 1..5 loop
    print arr[i]
end

for elem in arr loop
    print elem
end
```

### Test-case 6
```pascal
type Point is record
    var x : real
    var y : real
end

var p : Point
p.x := 3.0
p.y := 4.0

print "Coordinates: ", p.x, p.y
```

### Test-case 7
```pascal
routine factorial(n : integer) : integer
is
    var result : integer is 1
    for i in 2..n loop
        result := result * i
    end
    return result
end

var num : integer is factorial(5)
```

### Test-case 8
```pascal
routine printGreeting(name : string)
is
    print "Hello, ", name
end

printGreeting("Alice")
```

### Test-case 9
```pascal
routine B(n : integer)

routine A(n : integer)
is
    print "A calls B"
    B(n - 1)
end

routine B(n : integer)
is
    if n > 0 then
        print "B calls A"
        A(n)
    end
end
```

### Test-case 10
```pascal
var a : boolean is true
var b : boolean is false
var c : integer is 1

if a and (b or (c = 1)) then
    print "Condition met"
end

var d : integer is 10
var e : real is 3.14
var result : real is d * e + 2.5
```

### Test-case 11
```pascal
var intVal : integer is 5
var realVal : real is 2.71
var boolVal : boolean is true

intVal := realVal  -- округление до 3
realVal := boolVal -- станет 1.0
boolVal := intVal  -- ошибка, если intVal ≠ 0 или 1
```

### Test-case 12
```pascal
type Address is record
    var street : string
    var number : integer
end

type Person is record
    var name : string
    var addr : Address
end

var person : Person
person.name := "Tsimafei"
person.addr.street := "Hikalo st."
person.addr.number := 9
```

### Test-case 13
```pascal
routine factorial(n : integer) : integer
is
    if n <= 1 then
        return 1
    else
        return n * factorial(n - 1)
    end
end

routine main()
is
    var result : integer is factorial(5)
    print result
end
```

### Test-case 14
```pascal
var a : array[5] integer

for i 1..5 loop
    a[i] := i
end
```

### Test-case 15
```pascal
var n : integer is 5

while n > 0 loop
    print n; n := n - 1
end
```