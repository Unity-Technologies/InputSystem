// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var mrefCommon = require('./ManagedReference.common.js');
var extension = require('./ManagedReference.extension.js');
var overwrite = require('./ManagedReference.overwrite.js');

exports.transform = function (model) {
  if (overwrite && overwrite.transform) {
    return overwrite.transform(model);
  }

  if (extension && extension.preTransform) {
    model = extension.preTransform(model);
  }

  if (mrefCommon && mrefCommon.transform) {
    model = mrefCommon.transform(model);
  }
  if (model.type.toLowerCase() === "enum") {
    model.isClass = false;
    model.isEnum = true;
  }
  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);

  //Added to sort enums by value and add value itself to model
  //Derived from https://github.com/dotnet/docfx/issues/2285#
  if(model.isEnum) {
    var childrens = model.children[0].children;

    childrens.forEach(function (item) {
      const regex = /[\w\d]+\s?=\s?(\d+),?/gm;
      var m = regex.exec(item.syntax.content[0].value);
      if(m !== null) {
       item._enum_value = parseInt(m[1]); 
      }
    });

    const compare = function( a, b ) {
      if ( a._enum_value < b._enum_value ){
        return -1;
      }
      if ( a._enum_value > b._enum_value ){
        return 1;
      }
      return 0;
    }
    model.children[0].children = childrens.sort(compare);
  }
  
  //This extension mechanism doesn't appear to work -- if it did above enum code should be there instead of here.
  if (extension && extension.postTransform) {
    model = extension.postTransform(model);
  }

  return model;
}

exports.getOptions = function (model) {
  if (overwrite && overwrite.getOptions) {
    return overwrite.getOptions(model);
  }

  return {
    "bookmarks": mrefCommon.getBookmarks(model)
  };
}