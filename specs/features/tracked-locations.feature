Feature: Tracked locations

  A person adds a place by its name and lists the places that they track.
  The places are kept in a SQLite database file from one run to the next.

  Scenario: Add asks the geocoding service for the name
    Given the service answers one result named "Kathmandu" at 27.70169, 85.3206
    When a person runs "add Kathmandu"
    Then the program sent the request "GET https://geocoding-api.open-meteo.com/v1/search?name=Kathmandu"

  Scenario: Add stores the one match and reports it
    Given the service answers one result named "Kathmandu" at 27.70169, 85.3206
    When a person runs "add Kathmandu"
    Then standard output is "Added Kathmandu (27.70169, 85.3206)"
    And the exit code is 0

  Scenario: List prints a stored location
    Given the service answers one result named "Kathmandu" at 27.70169, 85.3206
    And a person ran "add Kathmandu" on the database file
    When a person runs "list" on the database file
    Then standard output is "1 Kathmandu (27.70169, 85.3206)"
    And the exit code is 0

  Scenario: List prints the locations in the order they were added
    Given the service answers one result named "Kathmandu" at 27.70169, 85.3206
    And a person ran "add Kathmandu" on the database file
    And the service answers one result named "Vaduz" at 47.14151, 9.52154
    And a person ran "add Vaduz" on the database file
    When a person runs "list" on the database file
    Then standard output is two lines, "1 Kathmandu (27.70169, 85.3206)" and "2 Vaduz (47.14151, 9.52154)"
    And the exit code is 0

  Scenario: List with no database file says nothing is tracked
    Given no file exists at the database path
    When a person runs "list" on the database file
    Then standard output is "No tracked locations."
    And the exit code is 0

  Scenario: Add fails when no place has the name
    Given the service answers no result
    When a person runs "add Zzzzqqqxxx"
    Then standard error holds a line that says no place has the name and holds "Zzzzqqqxxx"
    And the exit code is 1

  Scenario: Add fails when the name matches several places
    Given the service answers several results
    When a person runs "add Boston"
    Then standard error holds a line that says the name matches several places and holds "Boston"
    And the exit code is 1

  Scenario: Add fails when the service gives no answer
    Given the request gets no answer from the service
    When a person runs "add Kathmandu"
    Then standard error holds a line that says the service gave no answer
    And the exit code is 1

  Scenario Outline: A failed add prints nothing and changes nothing
    Given the service answers one result named "Kathmandu" at 27.70169, 85.3206
    And a person ran "add Kathmandu" on the database file
    And <failure>
    When a person runs "<command>" on the database file
    Then standard output is empty
    And the exit code is 1
    And "list" on the database file prints the same lines as before the add

    Examples:
      | failure                                     | command      |
      | the service answers no result               | add Zzzzqqqxxx |
      | the service answers several results         | add Boston   |
      | the request gets no answer from the service | add Vaduz    |

  Scenario: Add with no name sends no request
    When a person runs "add" with no name
    Then the program sent no request
    And standard error holds an error
    And the exit code is not 0

  Scenario Outline: Help names the database option and its default
    When a person runs "<command> --help"
    Then standard output holds the option "--database"
    And standard output holds the default value "forecast.db"

    Examples:
      | command |
      | add     |
      | list    |
