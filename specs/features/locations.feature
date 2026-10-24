Feature: Tracked locations

  Scenario: Adding a place sends the request to the geocoding service
    Given the service answers "Kathmandu" with one place "Kathmandu" at 27.70169, 85.3206
    When I run "add Kathmandu" on a database file
    Then the program sent the request "GET https://geocoding-api.open-meteo.com/v1/search?name=Kathmandu"

  Scenario: Adding a place prints it
    Given the service answers "Kathmandu" with one place "Kathmandu" at 27.70169, 85.3206
    When I run "add Kathmandu" on a database file
    Then standard output is "Added Kathmandu (27.70169, 85.3206)"
    And the exit code is 0

  Scenario: A listed place was added by an earlier run
    Given the service answers "Kathmandu" with one place "Kathmandu" at 27.70169, 85.3206
    And I ran "add Kathmandu" on a database file
    When I run "list" on a database file
    Then standard output is "1 Kathmandu (27.70169, 85.3206)"
    And the exit code is 0

  Scenario: Places are listed in the order they were added
    Given the service answers "Kathmandu" with one place "Kathmandu" at 27.70169, 85.3206
    And the service answers "Vaduz" with one place "Vaduz" at 47.14151, 9.52154
    And I ran "add Kathmandu" on a database file
    And I ran "add Vaduz" on a database file
    When I run "list" on a database file
    Then standard output is two lines
      | line                            |
      | 1 Kathmandu (27.70169, 85.3206) |
      | 2 Vaduz (47.14151, 9.52154)     |

  Scenario: Listing with no database file
    When I run "list" on a database file
    Then standard output is "No tracked locations."
    And the exit code is 0

  Scenario: A name that no place has
    Given the service answers "Zzzzqqqxxx" with no place
    When I run "add Zzzzqqqxxx" on a database file
    Then standard error says no place has the name "Zzzzqqqxxx"
    And the exit code is 1

  Scenario: A name that several places have
    Given the service answers "Boston" with several places
    When I run "add Boston" on a database file
    Then standard error says the name "Boston" matches several places
    And the exit code is 1

  Scenario: The service gives no answer
    Given the service gives no answer
    When I run "add Kathmandu" on a database file
    Then standard error says the service gave no answer
    And the exit code is 1

  Scenario Outline: A failed add changes nothing
    Given the service answers "Vaduz" with one place "Vaduz" at 47.14151, 9.52154
    And I ran "add Vaduz" on a database file
    And the service answers "Boston" with several places
    And the service answers "Zzzzqqqxxx" with no place
    And I ran "list" on a database file
    And the output was kept
    And the service answers or fails for "<name>"
    When I run "add <name>" on a database file
    Then standard output is empty
    And "list" on the database file prints the kept output

    Examples:
      | name       |
      | Zzzzqqqxxx |
      | Boston     |
      | Kathmandu  |

  Scenario: Adding with no name
    Given the service gives no answer
    When I run "add" on a database file
    Then the program sent no request
    And standard error is not empty
    And the exit code is not 0

  Scenario Outline: The help names the database option
    When I run "<command> --help"
    Then standard output holds "--database"
    And standard output holds "forecast.db"

    Examples:
      | command |
      | add     |
      | list    |
