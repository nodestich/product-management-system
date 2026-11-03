Feature: Product catalogue
  Products get a database-assigned six-digit identifier, are searchable by part
  of their name, and the filtered lists are paginated.

  Scenario: A new product receives a six-digit identifier
    When I create a product named "USB hub" priced 24.90 with 15 in stock
    Then the response is 201
    And the product identifier has 6 digits

  Scenario: Updating a product leaves its stock untouched
    Given a product named "Monitor" with 5 items in stock
    When I rename it to "Monitor 27 inches" priced 199.00
    Then the name is "Monitor 27 inches"
    And the stock is 5

  Scenario: Searching is case-insensitive and matches part of the name
    Given a product named "Wireless mouse" with 10 items in stock
    And a product named "Mechanical keyboard" with 10 items in stock
    When I search for "MOUSE"
    Then the search returns 1 result

  Scenario Outline: Invalid pagination is rejected
    When I search for "mouse" on page <page> with page size <pageSize>
    Then the request fails with status 400

    Examples:
      | page | pageSize |
      | 0    | 10       |
      | 1    | 51       |