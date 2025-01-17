//
//  rpcProtocol.swift
//  proxyd
//
//  Created by Albie Spriddell on 11/08/2023.
//

import Foundation

@objc(RpcProtocol) protocol RpcProtocol {
    func setProxy(_ url: String, reply: @escaping (Bool) -> Void)
    func clearProxies(reply: @escaping (Bool) -> Void)
    func getVersion(reply: @escaping (UInt16) -> Void)
    func getUid(reply: @escaping (String) -> Void)
}
