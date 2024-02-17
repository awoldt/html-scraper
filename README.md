# Parse HTML Pages and View Tag Information

This is a HTML page parser capable of accepting a URL, parsing the entire page, and returning data associated with all the tags present on the page. Currently, it supports the following tags:

- p
- a
- img   
- input 
- script
- style
- form
- h1-h6
- span
- pre 
- ul 
- ol 
- li 
- meta
- title

Every page that is parsed will be saved in AWS S3 in the form of a JSON file. Additionally, you have the option to download the JSON file to your local device.
