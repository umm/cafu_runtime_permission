﻿using System;
using UniRx;
using UnityEngine;

namespace CAFU.RuntimePermission.Domain.UseCase {

    public interface IRuntimePermissionHandler {

        bool HasPermission(UserAuthorization userAuthorization);

        IObservable<bool> RequestPermission(UserAuthorization userAuthorization);

    }

    public static class RuntimePermissionHandlerExtension {

        public static IObservable<bool> CreateRuntimePermissionDialogResultObservable(this IRuntimePermissionHandler runtimePermissionHandler, UserAuthorization userAuthorization) {
            return Observable
                // ReSharper disable once InvokeAsExtensionMethod
                .Merge(
                    // 1秒経ってもアプリケーションのフォーカスが外れなかった場合
                    // ダイアログが出なかったと見なして、その時点でのパーミッションを返す
                    // この場合、流れる値は現在の端末設定に依存する
                    Observable
                        .Timer(TimeSpan.FromSeconds(1.0))
                        .TakeUntil(
                            Observable.EveryApplicationFocus().Where(x => !x)
                        )
                        .Select(_ => runtimePermissionHandler.HasPermission(userAuthorization)),
                    // アプリケーションのフォーカスが外れた後に復帰してきた場合
                    // ダイアログが出たと見なして、その時点でのパーミッションを返す（値はユーザの選択によって変動しうる）
                    Observable
                        .EveryApplicationFocus()
                        .Buffer(2)
                        // フォーカスが外れる→フォーカスが戻ってくるの流れになったら OS ダイアログの何らかのボタンが押されたと見なせる
                        .Where(result => !result[0] && result[1])
                        .Take(1)
                        .Select(_ => runtimePermissionHandler.HasPermission(userAuthorization))
                );
        }

    }

}