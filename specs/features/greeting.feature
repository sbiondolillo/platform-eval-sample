Feature: Greeting

  Scenario: The greeter greets by name
    Given the name "Ada"
    When the greeter greets
    Then the greeting is "Hello, Ada!"

  Scenario: The console program shouts the greeting of a name
    When the console program runs with the arguments "Ada --shout"
    Then the console program prints "HELLO, ADA!"

  Scenario: The console program shouts the greeting of the world
    When the console program runs with the arguments "--shout"
    Then the console program prints "HELLO, WORLD!"

  Scenario: The help lists the option shout
    When the console program runs with the arguments "--help"
    Then the console program prints a line that contains "--shout"
