/*
 * Copyright 2021-2021 Robbyxp1 @ github.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */

export function StoreState(id, state)
{
    var storage = window.localStorage;
    var jstate = JSON.stringify(state);
    storage.setItem(id, jstate);
  //  console.log("Set storage " + id + "= '" + jstate + "'");
}

export function FetchState(id, defaultstate, writebackdefault = false)
{
    var storage = window.localStorage;
    var state = storage.getItem(id);
  //  console.log("Fetch storage " + id + "= '" + state + "'");
    if (state == null)
    {
        if (defaultstate != null)
        {
            if (writebackdefault)
                StoreState(id, defaultstate);
        }
        return defaultstate;
    }
    else
        return JSON.parse(state);
}

export function FetchNumber(id, defaultstate, writebackdefault = false)
{
    var ret = FetchState(id, defaultstate, writebackdefault);
    if (ret != null)
    {
        return parseInt(ret);
    }
    else
        return null;
}

export function FetchBool(id, defaultstate, writebackdefault = false)
{
    var ret = FetchState(id, defaultstate, writebackdefault);
    if (ret != null)
    {
        return ret == "1" || ret == "true" ? true : false;
    }
    else
        return null;
}
