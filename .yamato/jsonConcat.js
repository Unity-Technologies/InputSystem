var jsonConcat = require("json-concat");
 
jsonConcat({
    src: ["upm-ci~/packages-old/packages.json", "upm-ci~/packages/packages.json"],
    dest: "upm-ci~/packages/packages.json"
}, function (json) {
    console.log(json);
});