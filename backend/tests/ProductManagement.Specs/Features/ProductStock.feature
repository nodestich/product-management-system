Feature: Product stock
  Stock is changed only through the dedicated endpoints, and the value must
  never go negative or above the maximum.

  Background:
    Given a product named "Mechanical keyboard" with 8 items in stock

  Scenario: Decreasing stock within the available amount
    When I decrease the stock by 3
    Then the stock is 5

  Scenario: Decreasing more than available is refused
    When I try to decrease the stock by 20
    Then the request fails with status 409
    And the stock is 8

  Scenario: A zero quantity is rejected
    When I try to decrease the stock by 0
    Then the request fails with status 400

  Scenario: Stock cannot be pushed above the maximum
    Given a product named "Short cable" with 999 items in stock
    When I try to increase the stock by 5
    Then the request fails with status 409

  Scenario: An unknown product is reported as missing
    When I try to decrease the stock of product 999
    Then the request fails with status 404