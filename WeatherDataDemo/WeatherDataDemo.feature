Feature: WeatherData
Check if the weather data downloads

 Background:
  Given alteryx running at" http://devgallery.alteryx.com/"
  And I am logged in using "curator@alteryx.com" and "alteryx rocks!"
  And I publish the application "download weather data"  
  And I check if the application is "RequiresApproval"

Scenario Outline: publish and run Download weather data
When I run weather data app by choosing the location "<Location>" and select the date <Date>
Then I see the weather data app has the text message <result> 
Examples: 
| Location   | Date         | result                |
| KCOBOULD16 | "2013-12-25" | Download Weather Data |
| KCOESTES2  | "2013-12-25" | Download Weather Data | 
